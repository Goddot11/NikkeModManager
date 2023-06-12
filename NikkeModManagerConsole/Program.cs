using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NikkeModManagerCore;

namespace NikkeModManagerConsole; 

class Program {

    public static void Main(string[] args) {
        NikkeDataService dataService = new NikkeDataService();
        dataService.LoadData().Wait();

        PreviewEngine.PreviewEngine engine = new PreviewEngine.PreviewEngine(900, 900);
        new Task(engine.Run).Start();

        List<NikkeBundle> bundles = dataService.GetBundles();

        foreach (IGrouping<string, NikkeBundle> group in bundles.GroupBy(x => x.CharacterId)) {
            Logger.WriteLine($"{group.First().CharacterId} - {group.First().Name}");
            foreach (NikkeBundle bundle in group.OrderBy(bundle => bundle.SkinKey)) {
                Logger.WriteLine($"\t{bundle.Pose} - {bundle.SkinKey}");
            }
        }

        List<string> ids = bundles.Select(q => q.CharacterId).Distinct().ToList();

        Logger.WriteLine("Enter character id to preview");

        var missingBundles = bundles.Where(q => q.Pose=="idle" && NikkeDataHelper.GetName(q.CharacterId) == "???").OrderBy(q => q.CharacterId);
        foreach(var missingBundle in missingBundles){
            engine.PreviewBundle(missingBundle, "idle");
            Console.ReadKey();
        }

        while (true) {
            string input = Console.ReadLine()!;
            string[] parts = input.Split(" ");
            if (parts.Length < 2) continue;
            string id = parts[0];
            string pose = parts[1];
            string anim = parts.Length > 2 ? parts[2] : "";
            if (ids.Contains(id)) {
                NikkeBundle? bundle = bundles.FirstOrDefault(q => q.CharacterId == id && q.Pose == pose);
                if (bundle != null) {
                    engine.PreviewBundle(bundle, anim);
                } else {
                    Logger.WriteLine($"Couldn't find {id} {pose}");
                }
            }
        }
    }
}