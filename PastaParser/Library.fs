﻿namespace DnKR.PastaParse

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
            |> fun r -> r.Content.ReadAsStream()
            |> xml.Load
        

        let nodes = xml.ChildNodes[1].ChildNodes
        let pastaURL = nodes[random.Next(0, nodes.Count-3)].InnerText

        let html = new HtmlDocument()
        RequestAsync pastaURL
            |> Async.RunSynchronously
            |> fun r -> r.Content.ReadAsStream()
            |> html.Load

        let node = html.DocumentNode.SelectNodes("//div[@class='iCQ7N']").Item(0) // div класс с пастой

        let pasta = new Pasta(node.SelectNodes("//div[@class='afzSy']").Item(0).InnerText, // div класс с текстом пасты
            Seq.toArray (seq { for i in node.SelectNodes("//div[@class='e2n0B']").Item(0).ChildNodes do i.InnerHtml }), // div класс с тегами пасты
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
    let rec GetFilteredPastaAsync (filter: string[]) : Task<Pasta> = task {
        
        let! pasta = GetPastaAsync()

        match Seq.exists2 (fun a b -> String.Equals(a, b)) (filter |> seq) pasta.Tags with
        | true -> return! GetFilteredPastaAsync(filter)
        |_ -> return pasta

    }
