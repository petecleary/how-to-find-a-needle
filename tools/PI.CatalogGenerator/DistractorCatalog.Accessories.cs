namespace PI.CatalogGenerator;

// Telephony, audio, peripherals and bags: categories with no compatibility rule. They are the bulk of a real
// shop, and they give keyword and vector search something to wade through: words like "cordless", "battery",
// "laptop" and "charging" turn up here too, without the meaning a shopper is after.
public sealed partial class DistractorCatalog
{
    private static readonly string[] Colours = ["Black", "White", "Sage"];

    private void AddCordlessPhones()
    {
        string[] reviews =
        [
            "Clear call quality around the whole house.",
            "Big buttons are easy for my dad to use.",
            "Call blocking has stopped the nuisance calls.",
            "Setting up the handsets took two minutes.",
        ];

        (string Brand, string Name, string Description, decimal Price)[] phones =
        [
            ("Hollis", "Cordless Phone Single", "A DECT cordless home phone with one handset and a charging base.", 24),
            ("Hollis", "Cordless Phone with Answering Machine", "A DECT cordless phone with a built-in answering machine and nuisance call blocking.", 39),
            ("Dunmore", "DECT Phone Single", "A home phone with one DECT handset, a backlit keypad and a speakerphone.", 22),
            ("Dunmore", "DECT Phone Twin Pack", "Two DECT handsets with intercom between rooms.", 42),
            ("Dunmore", "DECT Phone with Answer Machine", "A DECT home phone with a 30-minute answer machine.", 36),
        ];

        foreach (var p in phones)
        {
            Add(p.Brand, p.Name, ["cordless-phones"], p.Price, p.Description, reviews);
        }
    }

    private void AddPhoneBatteries()
    {
        string[] reviews =
        [
            "Handset lasts all week again.",
            "Exact replacement for the originals.",
            "Arrived charged enough to use straight away.",
        ];

        (string Brand, string Name, string Description, int CapacityMah, decimal VoltageV, decimal Price)[] batteries =
        [
            ("Hollis", "Handset Battery Twin Pack", "Two replacement rechargeable packs for Hollis home phone handsets.", 750, 2.4m, 11),
            ("Dunmore", "DECT Handset Battery", "A replacement rechargeable pack for Dunmore DECT handsets.", 650, 3.6m, 7),
            ("Ampwise", "AAA NiMH Phone Batteries (4 Pack)", "Low self-discharge AAA rechargeable cells for home phone handsets.", 550, 1.2m, 7),
            ("Ampwise", "AA NiMH Phone Batteries (4 Pack)", "Low self-discharge AA rechargeable cells for home phone handsets.", 1300, 1.2m, 9),
        ];

        foreach (var b in batteries)
        {
            Add(b.Brand, b.Name, ["phone-batteries"], b.Price, b.Description, reviews,
                S("capacityMah", b.CapacityMah), S("voltageV", b.VoltageV));
        }
    }

    private void AddPhoneAccessories()
    {
        string[] reviews =
        [
            "Fits perfectly.",
            "Feels premium for the price.",
            "Holds firm on bumpy roads.",
            "Easy to fit without bubbles.",
            "Colour is slightly different from the photo.",
        ];

        (string Brand, string Name, string Description, decimal Price)[] accessories =
        [
            ("Hollis", "Clear Phone Case", "A slim transparent case with raised edges around the camera.", 9),
            ("Hollis", "Leather Wallet Phone Case", "A folio case with card slots and a magnetic clasp.", 19),
            ("Hollis", "Rugged Phone Case", "A shock-absorbing case with reinforced corners.", 17),
            ("Hollis", "Glass Screen Protector (3 Pack)", "Tempered glass screen protectors with an alignment frame.", 9),
            ("Hollis", "Wall Mount for Home Phones", "A bracket that holds a home phone base on the wall.", 7),
            ("Hollis", "Headset for Home Phones", "A hands-free headset with a 2.5mm plug for home phone handsets.", 14),
            ("Dunmore", "Car Phone Mount", "A dashboard mount with a suction cup and one-handed release.", 14),
            ("Dunmore", "Bike Phone Mount", "A handlebar mount with a silicone strap.", 12),
            ("Dunmore", "Desk Phone Stand", "An adjustable aluminium stand for video calls.", 12),
            ("Dunmore", "Phone Grip Ring", "A metal ring grip that doubles as a stand.", 5),
            ("Dunmore", "Waterproof Phone Pouch", "A sealed pouch for beaches and boat trips.", 8),
            ("Dunmore", "Phone Line Extension Cable 10m", "Extends a landline phone socket by ten metres.", 6),
            ("Dunmore", "Phone Socket Double Splitter", "Plugs two home phones into one wall socket.", 5),
            ("Pinegrove", "Phone Cleaning Kit", "A spray, microfibre cloth and port brushes.", 8),
            ("Pinegrove", "Pop-Out Phone Grip", "A collapsible grip that sticks to the back of a case.", 6),
        ];

        foreach (var a in accessories)
        {
            Add(a.Brand, a.Name, ["phone-accessories"], a.Price, a.Description, reviews);
        }

        foreach (var colour in Colours)
        {
            Add("Hollis", $"Slim Phone Case in {colour}", ["phone-accessories"], 12,
                $"A soft-touch silicone case in {colour.ToLowerInvariant()}.", reviews);
            Add("Dunmore", $"Kickstand Phone Case in {colour}", ["phone-accessories"], 15,
                $"A protective case in {colour.ToLowerInvariant()} with a fold-out kickstand for watching video.", reviews);
        }

        Add("Pinegrove", "Phone Sanitiser Box", ["phone-accessories"], 29,
            "A UV-C box that cleans a phone, keys and earbuds in five minutes.", reviews);
        Add("Pinegrove", "Phone Holder for Bed", ["phone-accessories"], 16,
            "A long flexible gooseneck arm that clamps to a headboard.", reviews);
    }

    private void AddAudio()
    {
        string[] reviews =
        [
            "Sound is clear with decent bass.",
            "Comfortable for hours.",
            "Pairs quickly with my phone.",
            "Battery lasts longer than advertised.",
            "Noise cancelling works well on the train.",
            "A bit quiet at maximum volume.",
        ];

        (string Brand, string Name, string Description, decimal Price)[] audio =
        [
            ("Solace", "True Wireless Earbuds", "Bluetooth earbuds with a pocket charging case and 24 hours of total playback.", 39),
            ("Solace", "Noise-Cancelling Earbuds", "Bluetooth earbuds with active noise cancelling and a transparency mode.", 79),
            ("Solace", "Sports Earbuds", "Sweat-resistant earbuds with ear hooks that stay put while running.", 44),
            ("Solace", "On-Ear Headphones", "Lightweight folding Bluetooth headphones.", 34),
            ("Solace", "Noise-Cancelling Headphones", "Over-ear Bluetooth headphones with active noise cancelling and 30-hour playback.", 119),
            ("Solace", "Mini Portable Speaker", "A palm-sized Bluetooth speaker with a carry strap.", 22),
            ("Solace", "Waterproof Speaker", "A rugged Bluetooth speaker that survives a dunk in the pool.", 49),
            ("Solace", "Soundbar", "A compact TV soundbar with an optical input and HDMI ARC.", 99),
            ("Solace", "Bookshelf Speakers (Pair)", "Powered bookshelf speakers with Bluetooth and an optical input.", 129),
            ("Solace", "Smart Speaker", "A voice-controlled speaker with Wi-Fi streaming.", 69),
            ("Solace", "DAB+ Radio", "A portable digital radio with presets and an alarm.", 49),
            ("Solace", "Replacement Ear Tips (3 Pairs)", "Silicone ear tips in small, medium and large.", 7),
            ("Solace", "Headphone Travel Case", "A hard zip case that holds folded over-ear headphones.", 14),
            ("Solace", "Bluetooth Audio Receiver", "Adds Bluetooth to an older stereo through a 3.5mm or RCA input.", 19),
            ("Solace", "Alarm Clock Speaker", "A bedside speaker with a clock, alarm and FM radio.", 34),
            ("Echofield", "USB Microphone", "A plug-and-play cardioid microphone for calls and streaming.", 49),
            ("Echofield", "Podcast Microphone Kit", "A microphone with a boom arm, pop filter and shock mount.", 89),
            ("Echofield", "Gaming Headset", "A wired headset with a detachable boom microphone.", 39),
            ("Echofield", "Wireless Gaming Headset", "A low-latency wireless headset with a USB dongle.", 79),
            ("Echofield", "Studio Monitor Headphones", "Closed-back wired headphones with a flat response for mixing.", 69),
            ("Echofield", "Desktop Speakers", "USB-powered desk speakers with a volume knob and headphone socket.", 29),
            ("Echofield", "Clip-On Lapel Microphone", "A lavalier microphone for recording interviews on a phone.", 17),
            ("Echofield", "USB Audio Interface", "A two-input audio interface for guitars and XLR microphones.", 99),
            ("Echofield", "Headphone Stand", "A weighted aluminium stand for over-ear headphones.", 19),
            ("Echofield", "Acoustic Foam Panels (12 Pack)", "Wedge foam panels that reduce echo in small rooms.", 22),
            ("Echofield", "Portable Recorder", "A handheld stereo recorder with built-in microphones.", 89),
        ];

        foreach (var a in audio)
        {
            Add(a.Brand, a.Name, ["audio"], a.Price, a.Description, reviews);
        }

        foreach (var colour in Colours)
        {
            Add("Solace", $"Buds Lite in {colour}", ["audio"], 24,
                $"Entry-level Bluetooth earbuds in {colour.ToLowerInvariant()}.", reviews);
            Add("Echofield", $"Wired Earphones in {colour}", ["audio"], 9,
                $"In-ear wired earphones with an inline microphone, in {colour.ToLowerInvariant()}.", reviews);
        }
    }

    private void AddPeripherals()
    {
        string[] reviews =
        [
            "Works straight out of the box.",
            "Feels well built.",
            "Comfortable to use all day.",
            "Receiver fits neatly inside the battery compartment.",
            "Would buy again.",
            "Software is basic but the hardware is good.",
        ];

        (string Brand, string Name, string Description, decimal Price)[] peripherals =
        [
            ("Pinegrove", "Wired Mouse", "A three-button optical mouse with a 1.8m cable.", 7),
            ("Pinegrove", "Vertical Mouse", "An upright mouse that keeps the wrist in a handshake position.", 24),
            ("Pinegrove", "Silent Wireless Mouse", "A wireless mouse with quiet click switches.", 14),
            ("Pinegrove", "Gaming Mouse", "A wired gaming mouse with adjustable DPI and side buttons.", 29),
            ("Pinegrove", "Bluetooth Mouse", "A multi-device Bluetooth mouse that switches between three computers.", 22),
            ("Tallis", "Ergonomic Mouse", "A contoured right-handed mouse with a thumb rest.", 39),
            ("Tallis", "Rechargeable Mouse", "A wireless mouse with a USB-C port, so there are no batteries to replace.", 27),
            ("Pinegrove", "Wireless Keyboard", "A full-size wireless keyboard with a number pad.", 24),
            ("Pinegrove", "Compact Keyboard", "A 75% layout keyboard that saves desk space.", 34),
            ("Pinegrove", "Bluetooth Keyboard", "A slim multi-device Bluetooth keyboard for tablets and computers.", 29),
            ("Tallis", "Split Ergonomic Keyboard", "A two-piece keyboard that lets each hand sit at shoulder width.", 119),
            ("Tallis", "Tenkeyless Mechanical Keyboard", "A mechanical keyboard without the number pad, with hot-swappable switches.", 79),
            ("Tallis", "Wireless Keyboard and Mouse Set", "A matching keyboard and mouse that share one USB receiver.", 34),
            ("Tallis", "4K Webcam", "A 4K webcam with autofocus and a privacy shutter.", 89),
            ("Tallis", "10-Inch Ring Light", "A desk ring light with a phone holder and three colour temperatures.", 22),
            ("Tallis", "Key Light", "A dimmable LED panel on a desk clamp for video calls.", 59),
            ("Corvid", "24-Inch Monitor", "A 24-inch full HD IPS monitor with HDMI and DisplayPort.", 119),
            ("Corvid", "27-Inch Monitor", "A 27-inch QHD IPS monitor with a height-adjustable stand.", 219),
            ("Corvid", "15.6-Inch Portable Monitor", "A slim second screen that runs from a USB-C video port.", 129),
            ("Corvid", "Aluminium Laptop Stand", "A fixed-height stand that raises a laptop screen to eye level.", 29),
            ("Corvid", "Dual Monitor Arm", "A desk-clamped gas-spring arm for two monitors.", 69),
            ("Corvid", "Vertical Laptop Stand", "A weighted cradle that stores a closed laptop upright.", 22),
            ("Corvid", "Large Desk Mat", "A 90cm felt desk mat that covers keyboard and mouse space.", 17),
            ("Corvid", "Cable Management Tray", "A steel tray that screws under a desk to hold plugs and cables.", 27),
            ("Pinegrove", "Mouse Mat", "A cloth mouse mat with a non-slip base.", 5),
            ("Tallis", "Portable Scanner", "A handheld page scanner that saves to a microSD card.", 59),
            ("Pinegrove", "Presentation Clicker", "A wireless presenter with slide buttons and a laser pointer.", 17),
            ("Pinegrove", "Small Drawing Tablet", "A pen tablet with pressure sensitivity for sketching.", 34),
            ("Tallis", "Pen Display Tablet", "A 13-inch screen you draw on directly with a pen.", 229),
            ("Tallis", "Game Controller", "A wired controller for PC games.", 24),
            ("Tallis", "Wireless Game Controller", "A Bluetooth controller with rumble and a rechargeable battery.", 44),
            ("Tallis", "LED Desk Lamp", "A dimmable desk lamp with a flexible arm.", 27),
            ("Tallis", "Monitor Light Bar", "A lamp that sits on top of a monitor and lights the desk, not the screen.", 34),
            ("Kestrel", "Dual-Slot SD Card Reader", "Reads SD and microSD cards at the same time over USB-A.", 12),
            ("Pinegrove", "Laptop Cooling Pad", "A tilted pad with two quiet fans, powered over USB.", 22),
            ("Pinegrove", "14-Inch Privacy Screen Filter", "A removable filter that hides the screen from side angles.", 29),
            ("Pinegrove", "Security Lock Cable", "A combination cable lock for the security slot on a computer.", 14),
        ];

        foreach (var p in peripherals)
        {
            Add(p.Brand, p.Name, ["peripherals"], p.Price, p.Description, reviews);
        }

        foreach (var colour in new[] { "Graphite", "White", "Pink" })
        {
            Add("Pinegrove", $"Wireless Mouse in {colour}", ["peripherals"], 17,
                $"A compact wireless mouse in {colour.ToLowerInvariant()}, with a USB receiver.", reviews);
        }
    }

    private void AddBags()
    {
        string[] reviews =
        [
            "Plenty of pockets.",
            "Straps are comfortable when fully loaded.",
            "Kept everything dry in a downpour.",
            "Zips feel solid.",
            "Smaller inside than it looks.",
        ];

        (string Brand, string Name, string Description, decimal Price)[] bags =
        [
            ("Ashcombe", "13-Inch Laptop Sleeve", "A padded zip sleeve for 13-inch laptops.", 17),
            ("Ashcombe", "15-Inch Laptop Sleeve", "A padded zip sleeve for 15-inch laptops.", 19),
            ("Ashcombe", "15-Inch Laptop Backpack", "A commuter backpack with a padded laptop section and a luggage strap.", 39),
            ("Ashcombe", "Canvas Messenger Bag", "A waxed canvas shoulder bag with a padded sleeve.", 44),
            ("Ashcombe", "Tech Organiser Pouch", "A zip pouch with elastic loops for cables, plugs and drives.", 16),
            ("Ashcombe", "Anti-Theft Backpack", "A backpack with hidden zips and a cut-resistant panel.", 54),
            ("Ashcombe", "Roll-Top Commuter Backpack", "A waterproof roll-top backpack for cycling to work.", 64),
            ("Ashcombe", "Tote Bag with Laptop Pocket", "A canvas tote with a padded inner pocket.", 32),
            ("Ashcombe", "Sling Bag", "A small crossbody bag for a phone, wallet and keys.", 24),
            ("Pinegrove", "11-Inch Tablet Sleeve", "A felt sleeve for an 11-inch tablet.", 12),
            ("Pinegrove", "Hard Drive Carry Case", "A hard case for a portable hard drive and its cable.", 9),
            ("Pinegrove", "Console Travel Bag", "A padded bag for a games console, controllers and cables.", 29),
            ("Pinegrove", "Accessory Pouch", "A zip pouch with mesh pockets for chargers and cables.", 11),
        ];

        foreach (var b in bags)
        {
            Add(b.Brand, b.Name, ["bags"], b.Price, b.Description, reviews);
        }
    }
}
