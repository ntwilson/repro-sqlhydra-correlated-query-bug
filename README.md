Reproduction of a regression in SqlHydra 4.0 for correlated subqueries used with aggregate functions. 

See the query in Program.fs:
```f#
    let innerQuery =
        select {
            for inner in dbo.CorrelatedQueryTestTable do
                correlate outer in dbo.CorrelatedQueryTestTable
                where (inner.field1 = outer.field1 && inner.field2 = outer.field2 && inner.field3 = 5.0)
                select inner.timestamp
        }

    let qry =
        select {
            for row in dbo.CorrelatedQueryTestTable do
                where (row.timestamp = maxBy (subqueryOne innerQuery))
                select row
        }
```

and the emitted SQL:
```SQL
SELECT "row".* FROM "dbo"."CorrelatedQueryTestTable" AS "row" WHERE ("row"."timestamp" = @p0)
```

with `@p0 = DateTime2: 1/1/0001 12:00:00 AM`

You can see the emitted SQL and parameters with `dotnet run`.

Also Claude discovered the bug from a failed unit test of ours and it wrote up its own description and proposed fix that you can view in bug-description.md.