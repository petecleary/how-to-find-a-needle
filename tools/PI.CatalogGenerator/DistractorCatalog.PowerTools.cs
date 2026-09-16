namespace PI.CatalogGenerator;

// Cordless tools, batteries and chargers: the battery-platform rule (ADR-0013). Generated tools use the curated
// platforms (Brakk 18V, Brakk 12V, Tornio 20V MAX), plus a third-party brand, Ferrox, whose packs are built for
// another brand's platform: a real-world source of "it says 18V, will it fit?" questions.
public sealed partial class DistractorCatalog
{
    private static readonly Dictionary<string, (string Label, int Volts)> Platforms = new()
    {
        ["brakk-18v"] = ("Brakk 18V", 18),
        ["brakk-12v"] = ("Brakk 12V", 12),
        ["tornio-20v-max"] = ("Tornio 20V MAX", 20),
    };

    private void AddDrills()
    {
        string[] reviews =
        [
            "Plenty of torque for putting up shelves.",
            "Balanced and not too heavy.",
            "The LED light is actually useful.",
            "Chuck holds bits firmly.",
            "Brushless motor makes a real difference to runtime.",
            "Wish it came with a belt clip.",
        ];

        // Every generated Brakk 18V product costs more than £100: GQ-04 asserts exactly six under £100.
        (string Brand, string Platform, string Name, string Tool, bool IsKit, decimal Price)[] drills =
        [
            ("Tornio", "tornio-20v-max", "Hammer Drill (Body Only)", "hammer drill", false, 109),
            ("Tornio", "tornio-20v-max", "Impact Driver (Body Only)", "impact driver", false, 99),
            ("Tornio", "tornio-20v-max", "Compact Drill Driver Kit (2x2.0Ah)", "compact drill driver", true, 159),
            ("Brakk", "brakk-12v", "Sub-Compact Drill Driver (Body Only)", "sub-compact drill driver", false, 59),
            ("Brakk", "brakk-18v", "Brushless Hammer Drill (Body Only)", "brushless hammer drill", false, 139),
            ("Brakk", "brakk-18v", "Brushless Impact Driver Kit (2x5.0Ah)", "brushless impact driver", true, 229),
        ];

        foreach (var d in drills)
        {
            var (platformLabel, volts) = Platforms[d.Platform];
            var contents = d.IsKit ? "Supplied with batteries, a charger and a carry case." : "Body only: battery and charger sold separately.";

            Add(d.Brand, $"{VoltageLabel(platformLabel)} {d.Name}", ["drills"], d.Price,
                $"A {d.Tool} for the {platformLabel} platform. {contents}",
                reviews,
                S("batteryPlatform", d.Platform), S("voltageV", volts));
        }
    }

    private void AddAngleGrinders()
    {
        string[] reviews =
        [
            "Cuts through rebar easily.",
            "Paddle switch feels safe.",
            "Guard adjusts without tools.",
        ];

        (string Brand, string Platform, string Name, bool IsKit, decimal Price)[] grinders =
        [
            ("Tornio", "tornio-20v-max", "Brushless 125mm Angle Grinder (Body Only)", false, 139),
            ("Brakk", "brakk-18v", "Brushless 125mm Angle Grinder (Body Only)", false, 149),
        ];

        foreach (var g in grinders)
        {
            var (platformLabel, volts) = Platforms[g.Platform];
            var contents = g.IsKit ? "Supplied with batteries, a charger and a carry case." : "Body only: battery and charger sold separately.";

            Add(g.Brand, $"{VoltageLabel(platformLabel)} {g.Name}", ["angle-grinders"], g.Price,
                $"An angle grinder for cutting and grinding metal and stone, on the {platformLabel} platform. {contents}",
                reviews,
                S("batteryPlatform", g.Platform), S("voltageV", volts));
        }
    }

    private void AddToolBatteries()
    {
        string[] reviews =
        [
            "Charge indicator is handy.",
            "Noticeably longer runtime than the smaller pack.",
            "Heavier, but worth it for the grinder.",
            "Clicks in securely.",
        ];

        (string Brand, string Platform, string Name, decimal CapacityAh, decimal Price)[] batteries =
        [
            ("Tornio", "tornio-20v-max", "2.0Ah Battery", 2.0m, 39),
            ("Tornio", "tornio-20v-max", "5.0Ah Battery", 5.0m, 69),
            ("Brakk", "brakk-12v", "4.0Ah Battery", 4.0m, 44),
        ];

        foreach (var b in batteries)
        {
            var (platformLabel, volts) = Platforms[b.Platform];

            Add(b.Brand, $"{VoltageLabel(platformLabel)} {b.Name}", ["batteries"], b.Price,
                $"A {b.CapacityAh:0.0}Ah battery pack for {platformLabel} tools.",
                reviews,
                S("platform", b.Platform), S("voltageV", volts), S("capacityAh", b.CapacityAh));
        }

        string[] thirdPartyReviews =
        [
            "Half the price of the branded one and lasts nearly as long.",
            "Fits snugly, fuel gauge works.",
            "Make sure you buy the right platform.",
        ];

        (string Platform, decimal CapacityAh, decimal Price)[] ferrox =
        [
            ("brakk-18v", 5.0m, 44),
            ("tornio-20v-max", 4.0m, 39),
        ];

        foreach (var f in ferrox)
        {
            var (platformLabel, volts) = Platforms[f.Platform];

            Add("Ferrox", $"{volts}V {f.CapacityAh:0.0}Ah Battery for {platformLabel} Tools", ["batteries"], f.Price,
                $"A third-party {f.CapacityAh:0.0}Ah battery pack built to fit {platformLabel} tools.",
                thirdPartyReviews,
                S("platform", f.Platform), S("voltageV", volts), S("capacityAh", f.CapacityAh));
        }
    }

    private void AddToolBatteryChargers()
    {
        string[] reviews =
        [
            "Charges a big pack in about an hour.",
            "Fan is a bit noisy.",
            "Wall-mountable, which saves bench space.",
        ];

        (string Brand, string Platform, string Name, string Description, decimal Price)[] chargers =
        [
            ("Tornio", "tornio-20v-max", "20V MAX Rapid Charger", "A fan-cooled rapid charger for Tornio 20V MAX battery packs.", 39),
            ("Brakk", "brakk-12v", "12V Charger", "A standard charger for Brakk 12V battery packs.", 22),
            ("Ferrox", "brakk-18v", "Charger for Brakk 18V Batteries", "A third-party charger built for Brakk 18V battery packs.", 19),
        ];

        foreach (var c in chargers)
        {
            var (_, volts) = Platforms[c.Platform];

            Add(c.Brand, c.Name, ["battery-chargers"], c.Price, c.Description, reviews,
                S("platform", c.Platform), S("voltageV", volts));
        }
    }

    // Tools with no category of their own in the taxonomy sit directly under "power-tools". No rule compares them,
    // so they carry their platform for display and filtering only.
    private void AddOtherPowerTools()
    {
        string[] reviews =
        [
            "Cuts cleanly and the blade change is quick.",
            "Much less hassle than a corded one.",
            "Dust extraction port fits my vacuum.",
            "A bit heavy for overhead work.",
        ];

        (string Brand, string Platform, string Name, string Description, decimal Price)[] tools =
        [
            ("Tornio", "tornio-20v-max", "165mm Circular Saw (Body Only)", "A circular saw for sheet material and timber up to 55mm deep.", 119),
            ("Tornio", "tornio-20v-max", "Reciprocating Saw (Body Only)", "A reciprocating saw for demolition and pruning cuts.", 104),
            ("Tornio", "tornio-20v-max", "Oscillating Multi-Tool (Body Only)", "An oscillating multi-tool for flush cuts, scraping and sanding.", 89),
            ("Tornio", "tornio-20v-max", "Brad Nailer (Body Only)", "An 18-gauge brad nailer for skirting and trim.", 189),
            ("Brakk", "brakk-18v", "Brushless 184mm Circular Saw (Body Only)", "A circular saw for timber up to 65mm deep.", 139),
            ("Brakk", "brakk-18v", "Reciprocating Saw (Body Only)", "A reciprocating saw with a tool-free blade clamp.", 124),
            ("Brakk", "brakk-12v", "Oscillating Multi-Tool (Body Only)", "A compact oscillating multi-tool for tight spaces.", 69),
            ("Brakk", "brakk-12v", "Rotary Tool (Body Only)", "A rotary tool for engraving, polishing and small cuts.", 59),
        ];

        foreach (var t in tools)
        {
            var (platformLabel, volts) = Platforms[t.Platform];

            Add(t.Brand, $"{VoltageLabel(platformLabel)} {t.Name}", ["power-tools"], t.Price,
                $"{t.Description} Runs on {platformLabel} battery packs, sold separately.",
                reviews,
                S("batteryPlatform", t.Platform), S("voltageV", volts));
        }
    }

    private void AddPowerToolAccessories()
    {
        string[] reviews =
        [
            "Good quality for the price.",
            "Sturdy and well made.",
            "Does the job.",
            "Case is flimsy but the contents are fine.",
            "Bought a second one for the van.",
        ];

        (string Name, string Description, decimal Price)[] accessories =
        [
            ("10-Piece HSS Drill Bit Set", "High-speed steel bits from 1mm to 10mm for metal, wood and plastic.", 9),
            ("5-Piece Masonry Drill Bit Set", "Carbide-tipped bits for brick, block and concrete.", 8),
            ("32-Piece Screwdriver Bit Set", "Phillips, Pozidriv, slotted and Torx bits with a quick-release holder.", 11),
            ("SDS Plus Chisel Set (3 Piece)", "Point, flat and wide chisels for breaking tile and plaster.", 17),
            ("165mm Circular Saw Blade (24T)", "A fast-cutting 24-tooth blade for rough timber.", 12),
            ("165mm Circular Saw Blade (40T)", "A 40-tooth blade for a cleaner finish in sheet material.", 15),
            ("Jigsaw Blade Set (10 Pack)", "Wood and metal jigsaw blades with a universal T-shank.", 7),
            ("Reciprocating Saw Blades (5 Pack)", "Bi-metal blades for wood with nails and metal pipe.", 9),
            ("125mm Sanding Discs P120 (25 Pack)", "Medium hook-and-loop sanding discs for general preparation.", 8),
            ("115mm Metal Cutting Discs (10 Pack)", "Thin cutting discs for steel bar and sheet.", 9),
            ("125mm Flap Discs (5 Pack)", "Zirconia flap discs for grinding welds and removing rust.", 12),
            ("9-Piece Hole Saw Kit", "Hole saws from 19mm to 64mm with an arbor, for wood and plasterboard.", 19),
            ("Ear Defenders", "Padded over-ear hearing protection with an adjustable headband.", 11),
            ("FFP2 Dust Masks (10 Pack)", "Disposable dust masks with a nose clip, for sanding and cutting.", 12),
            ("Gel Knee Pads", "Knee pads with gel inserts for flooring and tiling work.", 14),
            ("18-Inch Tool Bag", "A wide-mouth tool bag with inside and outside pockets.", 24),
            ("Wall-Mounted Tool Rack", "A steel rail with hooks for hanging tools in a garage.", 22),
            ("Leather Tool Belt", "A tool belt with a hammer loop and nail pouches.", 27),
            ("600mm Spirit Level", "An aluminium spirit level with three vials.", 11),
            ("30m Laser Distance Measure", "Measures distances, areas and volumes with a laser.", 29),
            ("Stud Detector", "Finds timber studs, metal and live wires behind walls.", 19),
            ("Combination Square", "A 300mm combination square for marking 90° and 45° angles.", 13),
            ("Folding Sawhorses (Pair)", "Two folding steel sawhorses with a 150kg load each.", 44),
            ("Quick-Release Bar Clamps (4 Pack)", "One-handed bar clamps for gluing and holding work.", 21),
        ];

        foreach (var a in accessories)
        {
            Add("Anvik", a.Name, ["power-tool-accessories"], a.Price, a.Description, reviews);
        }
    }

    // "Brakk 18V" → "18V", "Tornio 20V MAX" → "20V MAX": the platform label without the brand, for product names.
    private static string VoltageLabel(string platformLabel) => platformLabel[(platformLabel.IndexOf(' ') + 1)..];
}
