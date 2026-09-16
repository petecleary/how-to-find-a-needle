namespace PI.CatalogGenerator;

// Laptops and the things the charger, SSD and memory rules compare (ADR-0013). Generated laptops are extra
// target devices with a spread of ports, slots and memory, so the same accessory is compatible with one
// laptop and not another. Generated accessories are mostly near misses for the Blackbird Aerobook 14:
// few enough that the curated compatible products still rank near the top (see the ADR-0005 teaching notes).
public sealed partial class DistractorCatalog
{
    private void AddLaptops()
    {
        string[] reviews =
        [
            "Battery easily lasts a working day.",
            "Keyboard is comfortable for long typing sessions.",
            "Screen could be brighter outdoors.",
            "Fans get loud under load, otherwise quiet.",
            "Good value for what you get.",
            "Took a while to set up, but runs smoothly now.",
            "Trackpad is large and accurate.",
        ];

        (string Brand, string Model, string Kind, int ScreenIn, string Port, int MinWatts, string Slot, string Memory, decimal Price)[] laptops =
        [
            ("Halden", "Ridge 13", "thin-and-light laptop", 13, "usb-c", 45, "nvme", "ddr5-sodimm", 849),
            ("Halden", "Ridge 16", "thin-and-light laptop", 16, "usb-c", 100, "nvme", "ddr5-sodimm", 1249),
            ("Norland", "Fieldbook 14", "everyday laptop", 14, "barrel-5.5mm", 65, "sata", "ddr4-sodimm", 549),
            ("Norland", "Fieldbook 15 Plus", "everyday laptop", 15, "barrel-5.5mm", 90, "nvme", "ddr4-sodimm", 699),
            ("Merrow", "Studio 16", "creator laptop", 16, "usb-c", 140, "nvme", "ddr5-sodimm", 1899),
            ("Calder", "Classmate 11", "student laptop", 11, "barrel-5.5mm", 45, "sata", "ddr4-sodimm", 299),
            ("Calder", "Homebook 15", "home laptop", 15, "usb-c", 65, "nvme", "ddr4-sodimm", 499),
            ("Rookwood", "Vanta 17", "gaming laptop", 17, "barrel-5.5mm", 230, "nvme", "ddr5-sodimm", 1999),
        ];

        foreach (var l in laptops)
        {
            var port = l.Port == "usb-c" ? "a USB-C charging port" : "a round barrel charging socket";
            var slot = l.Slot == "nvme" ? "an NVMe M.2 slot" : "a SATA M.2 slot";
            var memory = l.Memory == "ddr5-sodimm" ? "DDR5 SO-DIMM memory" : "DDR4 SO-DIMM memory";
            var charging = l.Port == "usb-c"
                ? $"Charges from a USB-C Power Delivery charger of {l.MinWatts}W or more."
                : $"Comes with a {l.MinWatts}W charger in the box.";

            Add(l.Brand, l.Model, ["laptops"], l.Price,
                $"A {l.ScreenIn}-inch {l.Kind} with {port}, {slot} and {memory}. {charging}",
                reviews,
                S("chargingPort", l.Port), S("minChargerWattageW", l.MinWatts), S("m2SlotInterface", l.Slot),
                S("memoryType", l.Memory), S("screenSizeIn", l.ScreenIn));
        }
    }

    private void AddLaptopChargers()
    {
        string[] reviews =
        [
            "Runs warm but works well.",
            "Cable is a bit short.",
            "Replaced the original that came with my laptop.",
            "Solid build, no coil whine.",
            "Does what it says.",
        ];

        // For the Blackbird Aerobook 14 (USB-C, 65W or more) every generated laptop charger is a near miss, on
        // wattage (the 60W one misses by 5W) or on plug, which Stage 5 flags with a reason. More compatible
        // look-alikes would push the curated compatible chargers out of the top 3 the golden queries expect.
        foreach (var (brand, watts) in new[] { ("Ampwise", 45), ("Ampwise", 60), ("Tessel", 45) })
        {
            Add(brand, $"{watts}W USB-C PD Charger", ["laptop-chargers", "usb-c-pd-chargers"], 18 + watts * 0.35m,
                $"A {watts}W wall charger with one USB-C Power Delivery port and a detachable cable.",
                reviews,
                S("connector", "usb-c"), S("wattageW", watts), S("powerDelivery", true));
        }

        foreach (var (brand, watts) in new[] { ("Ampwise", 65), ("Tessel", 90) })
        {
            Add(brand, $"{watts}W Barrel Laptop Charger", ["laptop-chargers"], 16 + watts * 0.2m,
                $"A {watts}W replacement charger with a fixed 5.5mm barrel plug, for laptops with a round charging socket.",
                reviews,
                S("connector", "barrel-5.5mm"), S("wattageW", watts), S("powerDelivery", false));
        }
    }

    private void AddPhoneChargers()
    {
        string[] reviews =
        [
            "Charges my phone quickly.",
            "Tiny, fits in any bag.",
            "Two ports is handy for earbuds and phone overnight.",
            "Gets a little warm.",
            "Great value spare.",
        ];

        foreach (var (brand, watts) in new[] { ("Ampwise", 20), ("Ampwise", 30), ("Tessel", 25), ("Tessel", 35) })
        {
            Add(brand, $"{watts}W USB-C Wall Charger", ["phone-chargers", "usb-c-pd-chargers"], 8 + watts * 0.3m,
                $"A compact {watts}W wall charger with one USB-C Power Delivery port. Sized for phones, earbuds and smartwatches.",
                reviews,
                S("connector", "usb-c"), S("wattageW", watts), S("powerDelivery", true));
        }

        foreach (var (brand, watts) in new[] { ("Ampwise", 35), ("Tessel", 20) })
        {
            Add(brand, $"{watts}W Dual USB-C Wall Charger", ["phone-chargers", "usb-c-pd-chargers"], 12 + watts * 0.35m,
                $"A {watts}W wall charger with two USB-C Power Delivery ports that share the output. Sized for phones, earbuds and smartwatches.",
                reviews,
                S("connector", "usb-c"), S("wattageW", watts), S("powerDelivery", true));
        }
    }

    private void AddPowerBanks()
    {
        string[] reviews =
        [
            "Two full phone charges from one fill.",
            "Heavier than expected.",
            "Handy for festivals.",
            "Takes a long time to recharge itself.",
            "Display showing the percentage is useful.",
        ];

        (string Brand, string Name, int CapacityMah, int Watts, decimal Price)[] banks =
        [
            ("Ampwise", "10000mAh Power Bank", 10000, 20, 22),
            ("Ampwise", "26800mAh Power Bank", 26800, 65, 59),
            ("Ampwise", "Keyring Power Bank 3000mAh", 3000, 10, 12),
            ("Tessel", "5000mAh Mini Power Bank", 5000, 20, 17),
            ("Tessel", "20000mAh Power Bank with Display", 20000, 45, 44),
            ("Tessel", "Rugged Power Bank 15000mAh", 15000, 18, 38),
        ];

        foreach (var b in banks)
        {
            Add(b.Brand, b.Name, ["power-banks"], b.Price,
                $"A {b.CapacityMah}mAh portable battery with up to {b.Watts}W output, for topping up phones, tablets and handheld consoles away from a socket.",
                reviews,
                S("capacityMah", b.CapacityMah), S("wattageW", b.Watts));
        }
    }

    private void AddSsds()
    {
        string[] reviews =
        [
            "Cloned my old drive and swapped it in without trouble.",
            "Noticeably faster boot times.",
            "Runs cool.",
            "Heatsink not included, but not needed in my machine.",
            "Good capacity for the price.",
            "Check which slot your machine has before buying.",
        ];

        foreach (var (brand, label, gb, price) in new[] { ("Quarry", "500GB", 500, 44m) })
        {
            Add(brand, $"{label} NVMe M.2 2280 SSD", ["nvme-ssds"], price,
                $"A {label} solid-state drive for desktop PCs and games consoles, in the M.2 2280 size, connecting over NVMe.",
                reviews,
                S("interface", "nvme"), S("formFactor", "m2-2280"), S("capacityGb", gb));
        }

        Add("Quarry", "512GB SATA M.2 2280 SSD", ["sata-ssds"], 41,
            "A 512GB solid-state drive for desktop PCs in the M.2 2280 size, connecting over SATA.",
            reviews,
            S("interface", "sata"), S("formFactor", "m2-2280"), S("capacityGb", 512));
    }

    private void AddMemory()
    {
        string[] reviews =
        [
            "Recognised straight away, no settings to change.",
            "Multitasking is much smoother now.",
            "Fitted in five minutes.",
            "Matched pair, works in dual channel.",
        ];

        foreach (var (brand, label, gb, price) in new[] { ("Fathom", "8GB", 8, 22m), ("Linnet", "32GB Kit (2x16GB)", 32, 79m) })
        {
            Add(brand, $"{label} DDR4 SO-DIMM", ["so-dimm-memory"], price,
                $"{label} of DDR4 memory in the compact SO-DIMM size.",
                reviews,
                S("memoryType", "ddr4-sodimm"), S("capacityGb", gb));
        }

        foreach (var (brand, label, gb, price) in new[] { ("Fathom", "8GB", 8, 29m), ("Fathom", "64GB Kit (2x32GB)", 64, 189m), ("Linnet", "16GB", 16, 52m), ("Linnet", "32GB Kit (2x16GB)", 32, 96m) })
        {
            Add(brand, $"{label} DDR5 SO-DIMM", ["so-dimm-memory"], price,
                $"{label} of DDR5 memory in the compact SO-DIMM size.",
                reviews,
                S("memoryType", "ddr5-sodimm"), S("capacityGb", gb));
        }
    }

    private void AddExternalStorage()
    {
        string[] reviews =
        [
            "Fast transfers over USB-C.",
            "Tiny and light.",
            "Came formatted and ready to use.",
            "Survived a drop onto a hard floor.",
            "Great for backups.",
        ];

        foreach (var (label, gb, price) in new[] { ("1TB", 1000, 79m), ("4TB", 4000, 239m) })
        {
            Add("Quarry", $"{label} Portable SSD", ["external-storage"], price,
                $"A pocket-sized {label} external SSD that plugs in over USB-C, for backups and moving large files.",
                reviews,
                S("capacityGb", gb));
        }

        foreach (var (label, gb, price) in new[] { ("2TB", 2000, 59m), ("5TB", 5000, 109m) })
        {
            Add("Quarry", $"{label} Portable Hard Drive", ["external-storage"], price,
                $"A {label} external hard drive powered over USB, for backups and archives.",
                reviews,
                S("capacityGb", gb));
        }

        foreach (var (label, gb, price) in new[] { ("64GB", 64, 9m), ("256GB", 256, 24m) })
        {
            Add("Linnet", $"{label} USB-C Flash Drive", ["external-storage"], price,
                $"A {label} flash drive with a USB-C plug on a keyring loop.",
                reviews,
                S("capacityGb", gb));
        }

        foreach (var (label, gb, price) in new[] { ("32GB", 32, 7m), ("256GB", 256, 27m) })
        {
            Add("Kestrel", $"{label} USB Stick", ["external-storage"], price,
                $"A {label} USB-A memory stick with a sliding cap.",
                reviews,
                S("capacityGb", gb));
        }

        foreach (var (label, gb, price) in new[] { ("128GB", 128, 17m), ("256GB", 256, 29m) })
        {
            Add("Linnet", $"{label} microSD Card", ["external-storage"], price,
                $"A {label} microSD card with a full-size SD adapter, for cameras, drones and handheld consoles.",
                reviews,
                S("capacityGb", gb));
        }
    }

    private void AddUsbHubs()
    {
        string[] reviews =
        [
            "All ports work as described.",
            "Gets warm with everything plugged in.",
            "One cable to my desk setup now.",
            "Solid aluminium case.",
            "HDMI output works at 4K.",
        ];

        (string Brand, string Name, string Description, int? PassThroughW, decimal Price)[] hubs =
        [
            ("Tessel", "4-Port USB-A Hub", "Four USB-A ports from one, for keyboards, mice and flash drives.", null, 12),
            ("Tessel", "USB-C 7-in-1 Hub", "HDMI, Ethernet, three USB-A ports and card slots from one USB-C port, with 85W pass-through charging.", 85, 39),
            ("Tessel", "USB-C 10-in-1 Docking Station", "Two HDMI outputs, Ethernet, audio, card slots and five USB ports, with 100W pass-through charging.", 100, 89),
            ("Linnet", "USB-C to HDMI Converter", "Mirrors or extends a USB-C screen to a TV or monitor over HDMI at 4K.", null, 14),
            ("Linnet", "USB-C Ethernet Converter", "Adds a wired gigabit Ethernet socket through a USB-C port.", null, 16),
            ("Corvid", "USB-C Dual Display Dock", "Runs two external monitors from one USB-C cable, with Ethernet, USB ports and 100W pass-through charging.", 100, 129),
            ("Corvid", "Monitor Stand Hub", "A monitor riser with four USB ports and a card reader built into the front edge.", null, 49),
            ("Corvid", "Desk Clamp USB Hub", "A four-port hub that clamps to the desk edge, keeping ports within reach.", null, 24),
        ];

        foreach (var h in hubs)
        {
            Add(h.Brand, h.Name, ["usb-hubs"], h.Price, h.Description, reviews,
                h.PassThroughW is { } watts ? [S("wattageW", watts)] : []);
        }
    }

    private void AddCables()
    {
        string[] reviews =
        [
            "Sturdy braiding.",
            "Exactly the length I needed.",
            "Connectors feel solid.",
            "Cheaper than the big brands and works just as well.",
            "Tidied up my desk.",
        ];

        (string Brand, string Name, string Description, decimal Price)[] cables =
        [
            ("Pinegrove", "USB-C to USB-C Cable 1m", "A USB-C cable for charging and data, rated for 60W.", 6),
            ("Pinegrove", "USB-A to USB-C Cable 1m", "Connects USB-C devices to older USB-A ports and wall plugs.", 5),
            ("Pinegrove", "Braided USB-C Cable 240W 2m", "A braided USB-C cable rated for 240W, with a nylon sleeve that resists fraying.", 15),
            ("Pinegrove", "HDMI Cable 2m", "A high-speed HDMI cable supporting 4K at 60Hz.", 8),
            ("Pinegrove", "HDMI Cable 5m", "A long high-speed HDMI cable supporting 4K at 60Hz.", 13),
            ("Pinegrove", "DisplayPort Cable 2m", "A DisplayPort 1.4 cable for high refresh rate monitors.", 11),
            ("Pinegrove", "Cat6 Ethernet Cable 3m", "A snagless Cat6 network cable.", 6),
            ("Pinegrove", "Cat6 Ethernet Cable 10m", "A long snagless Cat6 network cable.", 12),
            ("Pinegrove", "4-Way Extension Lead 2m", "A four-socket mains extension lead with surge protection.", 16),
            ("Pinegrove", "6-Way Extension Lead with USB", "A six-socket mains extension lead with two built-in USB-A ports.", 26),
            ("Pinegrove", "3.5mm Audio Cable 1m", "A stereo aux cable for speakers, headphones and car stereos.", 4),
            ("Pinegrove", "Optical Audio Cable 2m", "A digital optical cable for soundbars and AV receivers.", 7),
            ("Pinegrove", "Micro-USB Cable 1m", "A micro-USB cable for older phones, controllers and accessories.", 4),
            ("Linnet", "Right-Angle USB-C Cable 1m", "A USB-C cable with an L-shaped plug that sits flat while gaming or in a car mount.", 8),
            ("Linnet", "Coiled USB-C Cable", "A springy coiled USB-C cable that stretches to 1.5m.", 11),
            ("Linnet", "USB 3.0 Extension Cable 2m", "Extends a USB-A port by two metres.", 6),
            ("Linnet", "VGA Cable 1.8m", "A VGA cable for projectors and older monitors.", 6),
            ("Linnet", "Retractable USB-C Cable", "A USB-C cable that winds back into its own case for travel.", 10),
            ("Linnet", "3-in-1 Charging Cable", "One USB-A plug that splits into USB-C, micro-USB and a phone connector.", 9),
            ("Ampwise", "100W USB-C Cable 2m", "A USB-C cable rated for 100W, with an e-marker chip.", 11),
            ("Ampwise", "USB-C Cable with Wattage Display", "A USB-C cable with a small screen in the plug that shows the power flowing through it.", 16),
            ("Pinegrove", "Cable Tidy Clips (20 Pack)", "Self-adhesive clips that hold cables along a desk edge.", 6),
            ("Pinegrove", "Cable Management Box", "A box that hides an extension lead and its tangle of plugs.", 18),
            ("Pinegrove", "IEC Kettle Lead 2m", "A replacement mains lead for monitors, desktop PCs and printers.", 6),
        ];

        foreach (var c in cables)
        {
            Add(c.Brand, c.Name, ["cables"], c.Price, c.Description, reviews);
        }
    }
}
