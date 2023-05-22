namespace json_csharp;
class Program
{
    
    static void Main(string[] args)
    {
        string json = File.ReadAllText(@"/home/RandomSymbols/fhdd/projects/work/json_csharp/txt.json");
        var parsed = JSON.ParseFrom(json);
        Console.WriteLine(parsed);
        Console.WriteLine(JSON.ParseFromObjectModel(parsed));
    }
}

