module SqlHydraCorrelatedQueryTest.Program

open SqlHydra.Query

let correlatedQueryWithMaxBy =
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

    let emitter: ISqlEmitter = SqliteEmitter()
    let compiled = emitter.EmitSelect(qry.SelectIR)
    printfn "%s\n\n  with parameters:%O" compiled.Sql compiled.Parameters

[<EntryPoint>]
let main _ =
    correlatedQueryWithMaxBy
    0
