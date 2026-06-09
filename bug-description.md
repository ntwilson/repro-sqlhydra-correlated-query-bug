Bug: Subquery parameters collide with outer query parameters, causing wrong values to be bound

  SqlHydra.Query version: 4.0.1

  Affected patterns:
  - Correlated subqueries using subqueryOne
  - IN subqueries
  - NOT IN subqueries

  ---
  Description

  When a query contains a subquery (e.g. subqueryOne (maxBy (Some col))), the emitted SQL has incorrect parameter bindings. The subquery parameter names collide with
  the outer query's parameter names, causing the subquery to receive the wrong runtime values. The most common symptom is the subquery silently returning NULL, which
  causes the outer WHERE clause to match zero rows.

  ---
  Root Cause

  In SqlEmitterBase.EmitValue, the SubQuery branch calls this.EmitSelectCore(ir), which allocates a fresh ParameterCollector starting at @p0. The subquery SQL is
  emitted with names @p0, @p1, etc. — the same names already used by the outer query.

  The parameters are then merged into the outer collector:

  | SubQuery ir ->
      let compiled = this.EmitSelectCore(ir)
      // Merge sub-query parameters into outer collector
      for (_, v) in compiled.Parameters do
          collector.Add(v) |> ignore    // <-- return value (outer name) is thrown away
      $"({compiled.Sql})"              // <-- SQL still has inner names @p0, @p1, ...

  collector.Add(v) returns the outer name (@p2, @p3, etc.), but that name is discarded. The embedded SQL fragment returned by this function still contains the inner
  names (@p0, @p1). When the database executes the query, the subquery's @p0 resolves to the outer query's @p0 — a completely different value.

  The same bug exists in EmitWhereInner for InSubQuery and NotInSubQuery.

  ---
  Concrete Example

  Given a query like:

  let maxTimestamp =
    select {
      for innerRow in table do
        correlate row in table
        where (innerRow.id = row.id && innerRow.timestamp <= runDateTime)
        select (maxBy (Some innerRow.timestamp))
    }

  selectWithConfig db {
    for row in table do
      where (row.date >= lowerBound && row.date <= upperBound && Some row.timestamp = subqueryOne maxTimestamp)
  }

  The outer query emits @p0 = lowerBound, @p1 = upperBound. The subquery's fresh collector emits @p0 = runDateTime. After merging, the combined parameter list is @p0 =
  lowerBound, @p1 = upperBound, @p2 = runDateTime, but the embedded subquery SQL still reads WHERE timestamp <= @p0. At execution time, @p0 is lowerBound (a date in
  the past), not runDateTime. The subquery returns NULL. The outer WHERE Some row.timestamp = NULL matches nothing.

  ---
  Fix

  In EmitValue (and the analogous InSubQuery/NotInSubQuery branches), replace the inner parameter names in the subquery SQL with the outer names returned by
  collector.Add. Sort by descending name length to prevent @p1 being replaced inside @p10:

  | SubQuery ir ->
      let compiled = this.EmitSelectCore(ir)
      let mutable sql = compiled.Sql
      for (innerName, v) in compiled.Parameters |> List.sortByDescending (fun (n, _) -> n.Length) do
          let outerName = collector.Add(v)
          sql <- sql.Replace(innerName, outerName)
      $"({sql})"

  Apply the same pattern to InSubQuery and NotInSubQuery in EmitWhereInner.

  ---
  How to Reproduce

  1. Create a table with columns id, date, and timestamp.
  2. Insert a few rows.
  3. Write a query with a correlated subquery that filters by a runtime parameter (e.g. WHERE timestamp <= @runDateTime), used via subqueryOne.
  4. Run against SQLite (in-memory). The outer query will return zero rows even though matching rows exist.

  The bug is database-agnostic: any database will bind @p0 in the subquery to the outer query's @p0 value.

  ---
  Workaround (until fixed): None without forking the library. The subquery parameter name collision happens entirely inside the emitter.
