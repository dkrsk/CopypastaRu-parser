namespace DnKR.PastaParse

open System.Xml
open System
open System.Threading.Tasks
open System.Net.Http
open HtmlAgilityPack



/// Структура с информацией о пасте
type Pasta = struct
    /// Текст пасты
    val Text : string
    /// Теги пасты
    val Tags : string[]
    // Ссылка на пасту
    val Url : string
    new (text : string, tags : string[], url : string) = { Text = text; Tags = tags; Url = url }

end

module PastaParser =

    ///Получает случайную пасту
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
            |> (fun r -> task { return! r.Content.ReadAsStreamAsync() }) |> Async.AwaitTask |> Async.RunSynchronously
            |> xml.Load
        

        let nodes = xml.ChildNodes[1].ChildNodes
        let pastaURL = nodes[random.Next(0, nodes.Count-3)].InnerText

        let html = new HtmlDocument()
        RequestAsync pastaURL
            |> Async.RunSynchronously
            |> fun r -> task { return! r.Content.ReadAsStreamAsync() } |> Async.AwaitTask |> Async.RunSynchronously
            |> html.Load

        let node = html.DocumentNode.SelectNodes("//div[@class='T7TZH']").Item(0) // div класс с пастой

        let pasta = new Pasta(node.SelectNodes("//div[@class='_6oyPp']").Item(0).InnerText, // div класс с текстом пасты
            Seq.toArray (seq { for i in node.SelectNodes("//div[@class='_50yka']").Item(0).ChildNodes do i.InnerHtml }), // div класс с тегами пасты
            pastaURL)
        // названия классов на сайте это вообще прикол)

        return pasta

    }

    // Получение текста случайной пасты
    let GetTextOnlyAsync () = task {
    
        let! pasta = GetPastaAsync()
        return pasta.Text

    }

    /// <summary>Получение пасты по фильтру</summary>
    /// <param name="filter">список исключающих тегов для получения пасты</param>
    let rec GetFilteredPastaAsync ([<ParamArray>]filter: string[]) : Task<Pasta> = task {
        
        let! pasta = GetPastaAsync()

        match Seq.exists (fun s -> Seq.exists  (fun t -> String.Equals(s,t)) pasta.Tags) filter with //idk why exist2 doesn't work
        | true -> return! GetFilteredPastaAsync(filter)
        |_ -> return pasta

    }
