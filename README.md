Reproduction of a regression in SqlHydra 4.0 for correlated subqueries used with aggregate functions. 

See the query in Program.fs:
```f#
    let innerQuery =
        select {
            for inner in dbo.CorrelatedQueryTestTable do
                correlate row in dbo.CorrelatedQueryTestTable

                where (inner.field1 = row.field1 && inner.field2 = row.field2 && inner.field3 < 10.0)

                select (maxBy inner.timestamp)
        }

    let qry =
        select {
            for row in dbo.CorrelatedQueryTestTable do
                where (row.field3 > 1.0 && row.timestamp = subqueryOne innerQuery)
                select row
        }
```

and the emitted SQL:
```SQL
SELECT "row".* FROM "dbo"."CorrelatedQueryTestTable" AS "row" WHERE (("row"."field3" > @p0) AND ("row"."timestamp" = (SELECT MAX("inner"."timestamp") FROM "dbo"."CorrelatedQueryTestTable" AS "inner" WHERE ((("inner"."field1" = "row"."field1") AND ("inner"."field2" = "row"."field2")) AND ("inner"."field3" < @p0)))))
```

with `@p0` = `Float: 1` and `@p1` = `Float: 10`

You can see the emitted SQL and parameters with `dotnet run`.

Also Claude discovered the bug from a failed unit test of ours and it wrote up its own description and proposed fix that you can view in bug-description.md.