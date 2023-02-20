namespace DnKR.PastaParse

open System.Xml
open System
open System.Threading.Tasks
open System.Net.Http
open HtmlAgilityPack



type Pasta = struct

    val Text : string
    val Tags : string[]
    val Url : string
    new (text : string, tags : string[], url : string) = { Text = text; Tags = tags; Url = url }

end

module PastaParser =

    let private GetPastaAsync () = task {
    
        let random = new Random(DateTime.Now.Subtract(new DateTime(2023,1,1)).TotalSeconds |> int)

        let RequestAsync (url: string)  = async {
            let client = new HttpClient()
            let! r = client.GetAsync(url)  |> Async.AwaitTask
            return r
        }
        
        let xml = new XmlDocument()
        RequestAsync "https://copypastas.ru/sitemap.xml"
            |> Async.RunSynchronously
            |> fun r -> r.Content.ReadAsStream()
            |> xml.Load
        

        let nodes = xml.ChildNodes[1].ChildNodes
        let pastaURL = nodes[random.Next(0, nodes.Count-3)].InnerText

        let html = new HtmlDocument()
        RequestAsync pastaURL
            |> Async.RunSynchronously
            |> fun r -> r.Content.ReadAsStream()
            |> html.Load

        let node = html.DocumentNode.SelectNodes("//div[@class='iCQ7N']").Item(0)

        let pasta = new Pasta(node.SelectNodes("//div[@class='afzSy']").Item(0).InnerText,
            Seq.toArray (seq { for i in node.SelectNodes("//div[@class='e2n0B']").Item(0).ChildNodes do i.InnerHtml }),
            pastaURL)
        

        return pasta

    }

    let GetTextOnlyAsync () = task {
    
        let! pasta = GetPastaAsync()
        return pasta.Text

    }

    let rec GetFilteredPastaAsync (filter: string[]) : Task<Pasta> = task {
        
        let! pasta = GetPastaAsync()

        match Seq.exists2 (fun a b -> String.Equals(a, b)) (filter |> seq) pasta.Tags with
        | true -> return! GetFilteredPastaAsync(filter)
        |_ -> return pasta

    }
