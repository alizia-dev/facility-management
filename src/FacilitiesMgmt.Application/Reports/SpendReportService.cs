using FacilitiesMgmt.Application.Abstractions;
using FacilitiesMgmt.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FacilitiesMgmt.Application.Reports;

public sealed record SpendReportQuery(DateTime FromUtc, DateTime ToUtc);

public sealed record SiteSpendDto(Guid SiteId, string SiteName, decimal TotalSpend, int CompletedRequests);

public sealed record SpendReportDto(
    DateTime FromUtc,
    DateTime ToUtc,
    decimal GrandTotal,
    IReadOnlyList<SiteSpendDto> Sites);

public sealed class SpendReportQueryValidator : AbstractValidator<SpendReportQuery>
{
    public SpendReportQueryValidator()
    {
        RuleFor(x => x.ToUtc)
            .GreaterThan(x => x.FromUtc)
            .WithMessage("'to' must be later than 'from'.");
    }
}

/// <summary>
/// "What did we spend at Site 12 last month?" — the question the spreadsheets could
/// not answer.
/// </summary>
public sealed class SpendReportService(IAppDbContext db)
{
    /// <summary>
    /// Total spend per site over a date range, for the caller's organisation only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>There is no IgnoreQueryFilters() here, and there does not need to be.</b>
    /// The instinct to reach for it on a reporting endpoint is that aggregating "across
    /// sites" sounds like it needs a wider view than a single-row lookup. It does not:
    /// the caller's organisation is already a fixed value established from their JWT,
    /// and the aggregate runs *within* that scope, grouped by site. Bypassing the filter
    /// would not widen the report's usefulness — it would turn a tenant report into a
    /// cross-tenant data leak, aggregated and pre-formatted. If a query in this codebase
    /// ever calls IgnoreQueryFilters(), treat it as a defect.
    /// </para>
    /// <para>
    /// <b>Spend is measured at completion.</b> Only Completed requests have an
    /// ActualCost, and CompletedAt is when the money was actually spent. Filtering on
    /// CreatedAt instead would answer a different and misleading question — work raised
    /// in March but completed in April would land in the wrong month.
    /// </para>
    /// <para>
    /// <b>The range is half-open:</b> <c>CompletedAt &gt;= from</c> and
    /// <c>CompletedAt &lt; to</c>. Inclusive-on-both-ends invites the classic
    /// last-millisecond-of-the-day bug, where a DATETIME2(3) value at 23:59:59.997 falls
    /// outside a range ending at 23:59:59. Half-open makes consecutive months partition
    /// cleanly with no double counting and no gap.
    /// </para>
    /// <para>
    /// Served by IX_Requests_Org_Site_CompletedAt — leading with OrganizationId (which
    /// the query filter always supplies), then SiteId for the grouping, then CompletedAt
    /// for the range seek, with ActualCost as an included column so the aggregate is
    /// covered and never touches the base table.
    /// </para>
    /// </remarks>
    public async Task<SpendReportDto> GetSiteSpendAsync(SpendReportQuery query, CancellationToken ct = default)
    {
        // Driven from Sites rather than grouping MaintenanceRequests, for two reasons.
        //
        // Correctness: a report that only lists sites which happen to have spend cannot
        // answer "what did we spend at Site 12 last month?" when the answer is "nothing"
        // — the site just vanishes, and absence reads as a bug rather than as zero.
        //
        // Translatability: GroupBy(r => new { r.SiteId, r.Site.Name }) reads perfectly
        // well and compiles, but EF cannot translate a grouping key that reaches through
        // a navigation property, and fails at runtime rather than at build. Correlated
        // aggregates over the navigation collection translate cleanly, and each one is a
        // seek on IX_Requests_Org_Site_CompletedAt with ActualCost included.
        var lines = await db.Sites
            .AsNoTracking()
            .Select(s => new SiteSpendDto(
                s.Id,
                s.Name,
                // ActualCost is non-null for every Completed request, but the column is
                // nullable, so the aggregate is too. Coalesce in SQL, not afterwards.
                s.MaintenanceRequests
                    .Where(r => r.Status == RequestStatus.Completed
                                && r.CompletedAt >= query.FromUtc
                                && r.CompletedAt < query.ToUtc)
                    .Sum(r => r.ActualCost) ?? 0m,
                s.MaintenanceRequests
                    .Count(r => r.Status == RequestStatus.Completed
                                && r.CompletedAt >= query.FromUtc
                                && r.CompletedAt < query.ToUtc)))
            .ToListAsync(ct);

        // Ordered after materialisation, on purpose. EF cannot translate an ORDER BY
        // over a member of a constructor-projected type, and the result is one row per
        // site — a handful, already in memory, with the expensive aggregation done in
        // SQL where it belongs. Sorting here costs nothing and keeps the query
        // translatable; if site counts ever reached the thousands this would become a
        // paged query ordered in SQL.
        var ordered = lines
            .OrderByDescending(s => s.TotalSpend)
            .ThenBy(s => s.SiteName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SpendReportDto(
            query.FromUtc,
            query.ToUtc,
            ordered.Sum(l => l.TotalSpend),
            ordered);
    }
}
