module SqlHydraCorrelatedQueryTest.Program

open SqlHydra.Query

let correlatedQueryWithMaxBy =
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

    let emitter: ISqlEmitter = SqliteEmitter()
    let compiled = emitter.EmitSelect(qry.SelectIR)
    printfn "%s\n\n  with parameters:%O" compiled.Sql compiled.Parameters

[<EntryPoint>]
let main _ =
    correlatedQueryWithMaxBy
    0
