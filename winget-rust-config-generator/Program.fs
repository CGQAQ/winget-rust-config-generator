// Learn more about F# at http://fsharp.org

open System
open System.Net
open System.Net.Http

open HtmlAgilityPack
open Fizzler.Systems.HtmlAgilityPack

            
let FetchMetaInfo =
    async {
        try
            let http = new HttpClient()
            let! r = http.GetStringAsync("https://forge.rust-lang.org/infra/other-installation-methods.html")
                     |> Async.AwaitTask
                     
            let document = new HtmlDocument()
            document.LoadHtml(r)
            let node = document.DocumentNode
            let version = node.QuerySelector("#content > main > table:nth-child(23) > thead > tr > th:nth-child(2)")
            printfn "%s" version.InnerText
        with
            | e -> printf "%s" e.Message;
                       
    }

[<EntryPoint>]
let main argv =
    FetchMetaInfo
        |> Async.RunSynchronously
        |> ignore
    0 // return an integer exit code



