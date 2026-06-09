using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chubb.PolicyManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedPolicies : Migration
    {
        // ─────────────────────────────────────────────────────────────────────
        // Seed records: 210 policies
        //   - All 4 statuses   (Active / Expired / Pending / Cancelled)
        //   - All 4 LoB        (Property / Casualty / A&H / Marine)
        //   - All 8 regions    (Singapore / Hong Kong / Australia / Japan /
        //                       Thailand / Indonesia / Malaysia / Philippines)
        //   - All 6 currencies (USD / SGD / HKD / AUD / JPY / THB)
        //   - Premium range    1 000 – 5 000 000
        //   - Effective/expiry spread: past, current, future → testable filtering
        //   - Idempotent: INSERT only when PolicyNumber does not already exist
        // ─────────────────────────────────────────────────────────────────────

        private static readonly (
            string PolicyNumber,
            string PolicyholderName,
            string LineOfBusiness,
            string Status,
            decimal PremiumAmount,
            string Currency,
            string EffectiveDate,
            string ExpiryDate,
            string Region,
            string Underwriter,
            bool FlaggedForReview
        )[] Seeds =
        [
            // ── Singapore / Property ──────────────────────────────────────────
            ("POL-000001", "Lim Hua Kee Trading Pte Ltd",        "Property",  "Active",    125000.00m,  "SGD", "2025-01-01", "2026-01-01", "Singapore",   "Sarah Tan",      false),
            ("POL-000002", "Ng Eng Building Materials",          "Property",  "Active",    340000.00m,  "SGD", "2025-03-15", "2026-03-15", "Singapore",   "James Wong",     false),
            ("POL-000003", "Tan Ah Kow Real Estate Sdn",         "Property",  "Expired",    87500.50m,  "SGD", "2023-06-01", "2024-06-01", "Singapore",   "Sarah Tan",      false),
            ("POL-000004", "Horizon Commercial Properties",      "Property",  "Active",   1200000.00m, "USD", "2024-07-01", "2025-07-01", "Singapore",   "Michael Chan",   true),
            ("POL-000005", "Marina Bay Retail Holdings",         "Property",  "Pending",   450000.00m,  "SGD", "2026-07-01", "2027-07-01", "Singapore",   "James Wong",     false),

            // ── Singapore / Casualty ──────────────────────────────────────────
            ("POL-000006", "Singapore Bus Alliance",             "Casualty",  "Active",    290000.00m,  "SGD", "2025-04-01", "2026-04-01", "Singapore",   "Rachel Lee",     false),
            ("POL-000007", "Orchard Road F&B Operators",         "Casualty",  "Expired",    56200.00m,  "SGD", "2023-01-01", "2024-01-01", "Singapore",   "Rachel Lee",     false),
            ("POL-000008", "Sentosa Hospitality Group",          "Casualty",  "Active",    780000.00m,  "USD", "2024-10-01", "2025-10-01", "Singapore",   "Michael Chan",   true),
            ("POL-000009", "SG Logistics Partners",              "Casualty",  "Cancelled",  33000.00m,  "SGD", "2024-02-01", "2025-02-01", "Singapore",   "James Wong",     false),

            // ── Singapore / A&H ──────────────────────────────────────────────
            ("POL-000010", "Raffles Medical Group",              "A&H",       "Active",    910000.00m,  "SGD", "2025-01-01", "2026-01-01", "Singapore",   "Rachel Lee",     false),
            ("POL-000011", "CapitaLand Employee Benefits Trust", "A&H",       "Active",   2450000.00m, "USD", "2024-06-01", "2025-06-01", "Singapore",   "Sarah Tan",      true),
            ("POL-000012", "Singapore Airlines Staff",           "A&H",       "Pending",   320000.00m,  "SGD", "2026-08-01", "2027-08-01", "Singapore",   "Michael Chan",   false),

            // ── Singapore / Marine ────────────────────────────────────────────
            ("POL-000013", "PSA International Shipping",         "Marine",    "Active",   3800000.00m, "USD", "2025-01-01", "2026-01-01", "Singapore",   "Jason Koh",      false),
            ("POL-000014", "Pacific Ocean Freight Pte Ltd",      "Marine",    "Active",   1650000.00m, "SGD", "2024-11-01", "2025-11-01", "Singapore",   "Jason Koh",      false),
            ("POL-000015", "Straits Tanker Holdings",            "Marine",    "Expired",   420000.00m,  "SGD", "2022-05-01", "2023-05-01", "Singapore",   "Jason Koh",      false),

            // ── Hong Kong / Property ──────────────────────────────────────────
            ("POL-000016", "Cheung Kong Property Holdings",      "Property",  "Active",   4900000.00m, "HKD", "2025-02-01", "2026-02-01", "Hong Kong",   "David Cheung",   false),
            ("POL-000017", "New World Development Ltd",          "Property",  "Active",   3200000.00m, "HKD", "2024-09-01", "2025-09-01", "Hong Kong",   "Linda Ho",       true),
            ("POL-000018", "Henderson Land Group",               "Property",  "Expired",   980000.00m,  "HKD", "2023-03-01", "2024-03-01", "Hong Kong",   "David Cheung",   false),
            ("POL-000019", "Sino Land Commercial Trust",         "Property",  "Pending",  2100000.00m, "HKD", "2026-06-01", "2027-06-01", "Hong Kong",   "Linda Ho",       false),
            ("POL-000020", "Wharf REIC Properties",              "Property",  "Active",   5000000.00m, "USD", "2024-12-01", "2025-12-01", "Hong Kong",   "David Cheung",   false),

            // ── Hong Kong / Casualty ──────────────────────────────────────────
            ("POL-000021", "MTR Corporation Ltd",                "Casualty",  "Active",   1800000.00m, "HKD", "2025-01-01", "2026-01-01", "Hong Kong",   "Victor Lam",     false),
            ("POL-000022", "Cathay Pacific Airlines",            "Casualty",  "Active",   4200000.00m, "USD", "2024-07-01", "2025-07-01", "Hong Kong",   "Linda Ho",       true),
            ("POL-000023", "HK Electric Investments",            "Casualty",  "Expired",   750000.00m,  "HKD", "2022-10-01", "2023-10-01", "Hong Kong",   "Victor Lam",     false),
            ("POL-000024", "Pacific Basin Shipping",             "Casualty",  "Cancelled",  88000.00m,  "HKD", "2024-04-01", "2025-04-01", "Hong Kong",   "David Cheung",   false),

            // ── Hong Kong / A&H ──────────────────────────────────────────────
            ("POL-000025", "AIA Group Ltd Staff Trust",          "A&H",       "Active",   3500000.00m, "HKD", "2025-03-01", "2026-03-01", "Hong Kong",   "Linda Ho",       false),
            ("POL-000026", "Hongkong Land Holdings Staff",       "A&H",       "Active",    640000.00m,  "HKD", "2024-10-01", "2025-10-01", "Hong Kong",   "Victor Lam",     false),
            ("POL-000027", "Vitasoy International Benefits",     "A&H",       "Pending",   210000.00m,  "HKD", "2026-09-01", "2027-09-01", "Hong Kong",   "Linda Ho",       false),

            // ── Hong Kong / Marine ────────────────────────────────────────────
            ("POL-000028", "Orient Overseas Container Line",     "Marine",    "Active",   4800000.00m, "USD", "2025-01-01", "2026-01-01", "Hong Kong",   "James Yip",      false),
            ("POL-000029", "China COSCO Shipping HK",            "Marine",    "Active",   3100000.00m, "HKD", "2024-08-01", "2025-08-01", "Hong Kong",   "James Yip",      true),
            ("POL-000030", "Gold Peak Industries Marine",        "Marine",    "Expired",   330000.00m,  "HKD", "2023-02-01", "2024-02-01", "Hong Kong",   "James Yip",      false),

            // ── Australia / Property ──────────────────────────────────────────
            ("POL-000031", "Westfield Group Pty Ltd",            "Property",  "Active",   2800000.00m, "AUD", "2025-01-01", "2026-01-01", "Australia",   "Emma Collins",   false),
            ("POL-000032", "Lendlease Corporation Ltd",          "Property",  "Active",   1950000.00m, "AUD", "2024-06-01", "2025-06-01", "Australia",   "Tom Bradley",    false),
            ("POL-000033", "GPT Group Commercial",               "Property",  "Expired",   890000.00m,  "AUD", "2022-11-01", "2023-11-01", "Australia",   "Emma Collins",   false),
            ("POL-000034", "Dexus Wholesale Property",           "Property",  "Pending",  3400000.00m, "AUD", "2026-07-01", "2027-07-01", "Australia",   "Tom Bradley",    false),
            ("POL-000035", "Charter Hall Group Assets",          "Property",  "Active",   1200000.00m, "AUD", "2025-04-01", "2026-04-01", "Australia",   "Emma Collins",   true),

            // ── Australia / Casualty ──────────────────────────────────────────
            ("POL-000036", "BHP Group Operations",               "Casualty",  "Active",   4700000.00m, "AUD", "2024-09-01", "2025-09-01", "Australia",   "Tom Bradley",    false),
            ("POL-000037", "Qantas Airways Ltd",                 "Casualty",  "Active",   3900000.00m, "USD", "2025-01-01", "2026-01-01", "Australia",   "Emma Collins",   false),
            ("POL-000038", "Rio Tinto Australia",                "Casualty",  "Expired",  1100000.00m, "AUD", "2022-06-01", "2023-06-01", "Australia",   "Tom Bradley",    false),
            ("POL-000039", "Wesfarmers Industrial",              "Casualty",  "Cancelled",  67500.00m,  "AUD", "2024-01-01", "2025-01-01", "Australia",   "Emma Collins",   false),

            // ── Australia / A&H ──────────────────────────────────────────────
            ("POL-000040", "Medibank Private Ltd",               "A&H",       "Active",   2200000.00m, "AUD", "2025-02-01", "2026-02-01", "Australia",   "Sophie Martin",  false),
            ("POL-000041", "Ramsay Health Care Ltd",             "A&H",       "Active",   1500000.00m, "AUD", "2024-07-01", "2025-07-01", "Australia",   "Sophie Martin",  true),
            ("POL-000042", "Healthscope Employee Benefits",      "A&H",       "Pending",   480000.00m,  "AUD", "2026-10-01", "2027-10-01", "Australia",   "Tom Bradley",    false),

            // ── Australia / Marine ────────────────────────────────────────────
            ("POL-000043", "Toll Holdings Marine",               "Marine",    "Active",   2600000.00m, "AUD", "2025-03-01", "2026-03-01", "Australia",   "Adam Fisher",    false),
            ("POL-000044", "Patrick Stevedores Operations",      "Marine",    "Active",   1800000.00m, "USD", "2024-10-01", "2025-10-01", "Australia",   "Adam Fisher",    false),
            ("POL-000045", "Qube Holdings Marine",               "Marine",    "Expired",   560000.00m,  "AUD", "2023-04-01", "2024-04-01", "Australia",   "Adam Fisher",    false),

            // ── Japan / Property ─────────────────────────────────────────────
            ("POL-000046", "Mitsui Fudosan Co Ltd",              "Property",  "Active",  498000000.00m,"JPY", "2025-01-01", "2026-01-01", "Japan",        "Kenji Yamamoto", false),
            ("POL-000047", "Sumitomo Realty Development",        "Property",  "Active",  312000000.00m,"JPY", "2024-08-01", "2025-08-01", "Japan",        "Yuki Tanaka",    false),
            ("POL-000048", "Tokyu Real Estate",                  "Property",  "Expired", 145000000.00m,"JPY", "2023-01-01", "2024-01-01", "Japan",        "Kenji Yamamoto", false),
            ("POL-000049", "Hankyu Hanshin Properties",          "Property",  "Pending", 220000000.00m,"JPY", "2026-07-01", "2027-07-01", "Japan",        "Yuki Tanaka",    false),
            ("POL-000050", "Mitsubishi Estate Co",               "Property",  "Active",  4800000.00m,  "USD", "2025-04-01", "2026-04-01", "Japan",        "Kenji Yamamoto", true),

            // ── Japan / Casualty ─────────────────────────────────────────────
            ("POL-000051", "Toyota Motor Corporation",           "Casualty",  "Active",  4500000.00m,  "USD", "2024-10-01", "2025-10-01", "Japan",        "Hiroshi Sato",   false),
            ("POL-000052", "Honda Motor Co Ltd",                 "Casualty",  "Active",  390000000.00m,"JPY", "2025-01-01", "2026-01-01", "Japan",        "Hiroshi Sato",   false),
            ("POL-000053", "Sony Group Corporation",             "Casualty",  "Expired", 210000000.00m,"JPY", "2022-09-01", "2023-09-01", "Japan",        "Yuki Tanaka",    false),
            ("POL-000054", "Panasonic Holdings",                 "Casualty",  "Cancelled", 950000.00m,  "USD", "2024-03-01", "2025-03-01", "Japan",        "Hiroshi Sato",   false),

            // ── Japan / A&H ──────────────────────────────────────────────────
            ("POL-000055", "Nippon Life Insurance Staff",        "A&H",       "Active",  280000000.00m,"JPY", "2025-06-01", "2026-06-01", "Japan",        "Mika Suzuki",    false),
            ("POL-000056", "Dai-ichi Life Employee Trust",       "A&H",       "Active",  165000000.00m,"JPY", "2024-11-01", "2025-11-01", "Japan",        "Mika Suzuki",    true),
            ("POL-000057", "Japan Post Insurance Benefits",      "A&H",       "Pending", 340000000.00m,"JPY", "2026-08-01", "2027-08-01", "Japan",        "Mika Suzuki",    false),

            // ── Japan / Marine ───────────────────────────────────────────────
            ("POL-000058", "Nippon Yusen Kabushiki Kaisha",      "Marine",    "Active",  4200000.00m,  "USD", "2025-02-01", "2026-02-01", "Japan",        "Takeshi Ito",    false),
            ("POL-000059", "Mitsui OSK Lines Ltd",               "Marine",    "Active",  380000000.00m,"JPY", "2024-07-01", "2025-07-01", "Japan",        "Takeshi Ito",    false),
            ("POL-000060", "K Line Shipping Japan",              "Marine",    "Expired",  95000000.00m,"JPY", "2023-05-01", "2024-05-01", "Japan",        "Takeshi Ito",    false),

            // ── Thailand / Property ───────────────────────────────────────────
            ("POL-000061", "Central Pattana Plc",                "Property",  "Active",   8900000.00m, "THB", "2025-01-01", "2026-01-01", "Thailand",     "Nattapong Chai", false),
            ("POL-000062", "Land and Houses Plc",                "Property",  "Active",   5600000.00m, "THB", "2024-09-01", "2025-09-01", "Thailand",     "Siriporn Kham",  false),
            ("POL-000063", "SC Asset Corporation",               "Property",  "Expired",  2300000.00m, "THB", "2023-03-01", "2024-03-01", "Thailand",     "Nattapong Chai", false),
            ("POL-000064", "Pruksa Holding Plc",                 "Property",  "Pending",  4100000.00m, "THB", "2026-06-01", "2027-06-01", "Thailand",     "Siriporn Kham",  false),
            ("POL-000065", "AP Thailand Plc",                    "Property",  "Active",    750000.00m,  "USD", "2025-05-01", "2026-05-01", "Thailand",     "Nattapong Chai", true),

            // ── Thailand / Casualty ───────────────────────────────────────────
            ("POL-000066", "PTT Exploration Production",         "Casualty",  "Active",  12000000.00m, "THB", "2024-10-01", "2025-10-01", "Thailand",     "Krit Wongchai",  false),
            ("POL-000067", "Siam Cement Group",                  "Casualty",  "Active",   9500000.00m, "THB", "2025-02-01", "2026-02-01", "Thailand",     "Krit Wongchai",  false),
            ("POL-000068", "Bangkok Dusit Medical",              "Casualty",  "Expired",  3200000.00m, "THB", "2022-08-01", "2023-08-01", "Thailand",     "Siriporn Kham",  false),
            ("POL-000069", "Thai Beverage Plc",                  "Casualty",  "Cancelled",  480000.00m, "USD", "2024-01-01", "2025-01-01", "Thailand",     "Krit Wongchai",  false),

            // ── Thailand / A&H ────────────────────────────────────────────────
            ("POL-000070", "Kasikorn Bank Staff Benefits",       "A&H",       "Active",   6800000.00m, "THB", "2025-03-01", "2026-03-01", "Thailand",     "Siriporn Kham",  false),
            ("POL-000071", "SCB Group Employee Trust",           "A&H",       "Active",   4900000.00m, "THB", "2024-08-01", "2025-08-01", "Thailand",     "Siriporn Kham",  true),
            ("POL-000072", "Muangthai Life Benefits",            "A&H",       "Pending",  2100000.00m, "THB", "2026-09-01", "2027-09-01", "Thailand",     "Nattapong Chai", false),

            // ── Thailand / Marine ─────────────────────────────────────────────
            ("POL-000073", "Thai Oil Plc Marine",                "Marine",    "Active",   1800000.00m, "USD", "2025-01-01", "2026-01-01", "Thailand",     "Krit Wongchai",  false),
            ("POL-000074", "Precious Shipping Plc",              "Marine",    "Active",   7200000.00m, "THB", "2024-06-01", "2025-06-01", "Thailand",     "Krit Wongchai",  false),
            ("POL-000075", "Regional Container Lines TH",        "Marine",    "Expired",  1900000.00m, "THB", "2023-07-01", "2024-07-01", "Thailand",     "Krit Wongchai",  false),

            // ── Indonesia / Property ──────────────────────────────────────────
            ("POL-000076", "PT Pakuwon Jati Tbk",                "Property",  "Active",   3800000000.00m, "IDR", "2025-01-01", "2026-01-01", "Indonesia",   "Budi Santoso",   false),
            ("POL-000077", "PT Summarecon Agung",                "Property",  "Active",    890000.00m, "USD", "2024-10-01", "2025-10-01", "Indonesia",   "Dewi Kusuma",    false),
            ("POL-000078", "PT Ciputra Development",             "Property",  "Expired",   430000.00m, "USD", "2023-04-01", "2024-04-01", "Indonesia",   "Budi Santoso",   false),
            ("POL-000079", "PT Agung Podomoro Land",             "Property",  "Pending",   650000.00m, "USD", "2026-07-01", "2027-07-01", "Indonesia",   "Dewi Kusuma",    false),
            ("POL-000080", "PT Modernland Realty",               "Property",  "Active",    280000.00m, "USD", "2025-06-01", "2026-06-01", "Indonesia",   "Budi Santoso",   true),

            // ── Indonesia / Casualty ──────────────────────────────────────────
            ("POL-000081", "PT Astra International Tbk",         "Casualty",  "Active",   2100000.00m, "USD", "2025-03-01", "2026-03-01", "Indonesia",   "Rizky Pratama",  false),
            ("POL-000082", "PT Telkom Indonesia",                "Casualty",  "Active",   1650000.00m, "USD", "2024-08-01", "2025-08-01", "Indonesia",   "Rizky Pratama",  false),
            ("POL-000083", "PT Bank Central Asia",               "Casualty",  "Expired",   540000.00m, "USD", "2022-12-01", "2023-12-01", "Indonesia",   "Dewi Kusuma",    false),
            ("POL-000084", "PT Pertamina Gas Marine",            "Casualty",  "Cancelled",  95000.00m, "USD", "2024-05-01", "2025-05-01", "Indonesia",   "Rizky Pratama",  false),

            // ── Indonesia / A&H ───────────────────────────────────────────────
            ("POL-000085", "PT Mandiri Inhealth",                "A&H",       "Active",    780000.00m, "USD", "2025-01-01", "2026-01-01", "Indonesia",   "Dewi Kusuma",    false),
            ("POL-000086", "PT Manulife Indonesia Benefits",     "A&H",       "Active",    560000.00m, "USD", "2024-07-01", "2025-07-01", "Indonesia",   "Dewi Kusuma",    true),
            ("POL-000087", "PT Allianz Life Indonesia",          "A&H",       "Pending",   310000.00m, "USD", "2026-10-01", "2027-10-01", "Indonesia",   "Budi Santoso",   false),

            // ── Indonesia / Marine ────────────────────────────────────────────
            ("POL-000088", "PT Berlian Laju Tanker",             "Marine",    "Active",   1200000.00m, "USD", "2025-02-01", "2026-02-01", "Indonesia",   "Eko Wibowo",     false),
            ("POL-000089", "PT Samudera Indonesia Tbk",          "Marine",    "Active",    890000.00m, "USD", "2024-09-01", "2025-09-01", "Indonesia",   "Eko Wibowo",     false),
            ("POL-000090", "PT Indo Trans Logistics",            "Marine",    "Expired",   230000.00m, "USD", "2023-06-01", "2024-06-01", "Indonesia",   "Eko Wibowo",     false),

            // ── Malaysia / Property ───────────────────────────────────────────
            ("POL-000091", "IGB Real Estate Investment",         "Property",  "Active",   3600000.00m, "MYR", "2025-01-01", "2026-01-01", "Malaysia",    "Ahmad Razali",   false),
            ("POL-000092", "SP Setia Bhd Group",                 "Property",  "Active",    920000.00m, "USD", "2024-11-01", "2025-11-01", "Malaysia",    "Faridah Malik",  false),
            ("POL-000093", "Sunway Real Estate Trust",           "Property",  "Expired",   480000.00m, "USD", "2023-02-01", "2024-02-01", "Malaysia",    "Ahmad Razali",   false),
            ("POL-000094", "IOI Properties Group",               "Property",  "Pending",   750000.00m, "USD", "2026-08-01", "2027-08-01", "Malaysia",    "Faridah Malik",  false),
            ("POL-000095", "KLCC Stapled Group",                 "Property",  "Active",  2800000.00m,  "MYR", "2025-05-01", "2026-05-01", "Malaysia",    "Ahmad Razali",   true),

            // ── Malaysia / Casualty ───────────────────────────────────────────
            ("POL-000096", "Petronas Operations Malaysia",       "Casualty",  "Active",   3200000.00m, "USD", "2025-01-01", "2026-01-01", "Malaysia",    "Zulkifli Hassan", false),
            ("POL-000097", "Tenaga Nasional Bhd",                "Casualty",  "Active",   1800000.00m, "MYR", "2024-06-01", "2025-06-01", "Malaysia",    "Zulkifli Hassan", false),
            ("POL-000098", "Malayan Banking Group",              "Casualty",  "Expired",   620000.00m, "USD", "2022-10-01", "2023-10-01", "Malaysia",    "Faridah Malik",   false),
            ("POL-000099", "CIMB Group Holdings",                "Casualty",  "Cancelled", 110000.00m, "USD", "2024-03-01", "2025-03-01", "Malaysia",    "Zulkifli Hassan", false),

            // ── Malaysia / A&H ────────────────────────────────────────────────
            ("POL-000100", "Great Eastern Life Malaysia",        "A&H",       "Active",   1400000.00m, "MYR", "2025-04-01", "2026-04-01", "Malaysia",    "Faridah Malik",   false),
            ("POL-000101", "Prudential Assurance Malaysia",      "A&H",       "Active",   1100000.00m, "MYR", "2024-09-01", "2025-09-01", "Malaysia",    "Faridah Malik",   true),
            ("POL-000102", "Hong Leong Assurance Benefits",      "A&H",       "Pending",   380000.00m, "MYR", "2026-11-01", "2027-11-01", "Malaysia",    "Ahmad Razali",    false),

            // ── Malaysia / Marine ─────────────────────────────────────────────
            ("POL-000103", "MISC Bhd Tankers",                   "Marine",    "Active",   3900000.00m, "USD", "2025-02-01", "2026-02-01", "Malaysia",    "Rahim Nordin",    false),
            ("POL-000104", "Bumi Armada Marine Bhd",             "Marine",    "Active",   2100000.00m, "USD", "2024-07-01", "2025-07-01", "Malaysia",    "Rahim Nordin",    false),
            ("POL-000105", "Malaysia Shipping Corporation",      "Marine",    "Expired",   670000.00m, "USD", "2023-08-01", "2024-08-01", "Malaysia",    "Rahim Nordin",    false),

            // ── Philippines / Property ────────────────────────────────────────
            ("POL-000106", "Ayala Land Inc",                     "Property",  "Active",  46000000.00m, "PHP", "2025-01-01", "2026-01-01", "Philippines", "Maria Santos",    false),
            ("POL-000107", "SM Prime Holdings Inc",              "Property",  "Active",   1200000.00m, "USD", "2024-08-01", "2025-08-01", "Philippines", "Jose Reyes",      false),
            ("POL-000108", "Megaworld Corporation",              "Property",  "Expired",   540000.00m, "USD", "2023-05-01", "2024-05-01", "Philippines", "Maria Santos",    false),
            ("POL-000109", "Robinsons Land Corporation",         "Property",  "Pending",   870000.00m, "USD", "2026-07-01", "2027-07-01", "Philippines", "Jose Reyes",      false),
            ("POL-000110", "Vista Land Holdings",                "Property",  "Active",   1800000.00m, "USD", "2025-03-01", "2026-03-01", "Philippines", "Maria Santos",    true),

            // ── Philippines / Casualty ────────────────────────────────────────
            ("POL-000111", "PLDT Communications Inc",            "Casualty",  "Active",   2400000.00m, "USD", "2025-02-01", "2026-02-01", "Philippines", "Ramon Cruz",      false),
            ("POL-000112", "Philippine Airlines Holdings",       "Casualty",  "Active",   3100000.00m, "USD", "2024-10-01", "2025-10-01", "Philippines", "Ramon Cruz",      false),
            ("POL-000113", "Globe Telecom Inc",                  "Casualty",  "Expired",   760000.00m, "USD", "2022-07-01", "2023-07-01", "Philippines", "Jose Reyes",      false),
            ("POL-000114", "BDO Unibank Operations",             "Casualty",  "Cancelled", 145000.00m, "USD", "2024-04-01", "2025-04-01", "Philippines", "Ramon Cruz",      false),

            // ── Philippines / A&H ─────────────────────────────────────────────
            ("POL-000115", "Philippine Health Insurance",        "A&H",       "Active",  38000000.00m, "PHP", "2025-01-01", "2026-01-01", "Philippines", "Jose Reyes",      false),
            ("POL-000116", "Insular Life Benefits Trust",        "A&H",       "Active",   1500000.00m, "USD", "2024-06-01", "2025-06-01", "Philippines", "Jose Reyes",      true),
            ("POL-000117", "Sun Life Philippines Benefits",      "A&H",       "Pending",   640000.00m, "USD", "2026-10-01", "2027-10-01", "Philippines", "Maria Santos",    false),

            // ── Philippines / Marine ──────────────────────────────────────────
            ("POL-000118", "International Container Terminal",   "Marine",    "Active",   2800000.00m, "USD", "2025-04-01", "2026-04-01", "Philippines", "Leo Navarro",     false),
            ("POL-000119", "Aboitiz Transport System",           "Marine",    "Active",   1600000.00m, "USD", "2024-11-01", "2025-11-01", "Philippines", "Leo Navarro",     false),
            ("POL-000120", "Stolt-Nielsen Philippines",          "Marine",    "Expired",   410000.00m, "USD", "2023-09-01", "2024-09-01", "Philippines", "Leo Navarro",     false),

            // ── Second wave — mixed regions, varied dates, all LoB ────────────
            // Expiring soon (within 90 days of 2026-06-08)
            ("POL-000121", "DBS Bank Singapore",                 "Casualty",  "Active",   1800000.00m, "SGD", "2025-07-01", "2026-07-01", "Singapore",   "Rachel Lee",      false),
            ("POL-000122", "OCBC Bank Holdings",                 "Casualty",  "Active",    920000.00m, "SGD", "2025-07-15", "2026-07-15", "Singapore",   "Sarah Tan",       false),
            ("POL-000123", "UOB Group Operations",               "Casualty",  "Active",   1100000.00m, "SGD", "2025-08-01", "2026-08-01", "Singapore",   "Michael Chan",    true),
            ("POL-000124", "ANZ Bank Australia",                 "A&H",       "Active",    760000.00m, "AUD", "2025-07-01", "2026-07-01", "Australia",   "Sophie Martin",   false),
            ("POL-000125", "NAB Insurance Benefits",             "A&H",       "Active",    640000.00m, "AUD", "2025-08-15", "2026-08-15", "Australia",   "Sophie Martin",   false),
            ("POL-000126", "Westpac Banking Group",              "Property",  "Active",   2200000.00m, "AUD", "2025-07-01", "2026-07-01", "Australia",   "Tom Bradley",     false),
            ("POL-000127", "Commonwealth Bank CRE",              "Property",  "Active",   1900000.00m, "AUD", "2025-08-01", "2026-08-01", "Australia",   "Emma Collins",    false),
            ("POL-000128", "HKEX Operations Ltd",                "Casualty",  "Active",    480000.00m, "HKD", "2025-07-01", "2026-07-01", "Hong Kong",   "Victor Lam",      false),
            ("POL-000129", "Bank of East Asia",                  "A&H",       "Active",    320000.00m, "HKD", "2025-06-15", "2026-06-15", "Hong Kong",   "Linda Ho",        true),
            ("POL-000130", "Mizuho Bank Japan",                  "Casualty",  "Active",  180000000.00m,"JPY", "2025-07-01", "2026-07-01", "Japan",       "Hiroshi Sato",    false),

            // Far-future policies (effective 2027+)
            ("POL-000131", "Keppel Corporation Singapore",       "Property",  "Pending",  3100000.00m, "SGD", "2027-01-01", "2028-01-01", "Singapore",   "Jason Koh",       false),
            ("POL-000132", "Sembcorp Industries",                "Marine",    "Pending",  2400000.00m, "SGD", "2027-03-01", "2028-03-01", "Singapore",   "Jason Koh",       false),
            ("POL-000133", "Fosun International HK",             "Property",  "Pending",  4600000.00m, "HKD", "2027-01-01", "2028-01-01", "Hong Kong",   "David Cheung",    false),
            ("POL-000134", "CK Hutchison Holdings",              "Casualty",  "Pending",  5000000.00m, "HKD", "2027-06-01", "2028-06-01", "Hong Kong",   "Victor Lam",      false),
            ("POL-000135", "Woodside Energy Australia",          "Marine",    "Pending",  3800000.00m, "AUD", "2027-02-01", "2028-02-01", "Australia",   "Adam Fisher",     false),
            ("POL-000136", "Origin Energy Australia",            "Casualty",  "Pending",  2900000.00m, "AUD", "2027-07-01", "2028-07-01", "Australia",   "Tom Bradley",     false),
            ("POL-000137", "Marubeni Corporation Japan",         "Property",  "Pending",250000000.00m, "JPY", "2027-04-01", "2028-04-01", "Japan",       "Kenji Yamamoto",  false),
            ("POL-000138", "KDDI Corporation Japan",             "Casualty",  "Pending",170000000.00m, "JPY", "2027-01-01", "2028-01-01", "Japan",       "Hiroshi Sato",    false),
            ("POL-000139", "PTT Global Chemical Thailand",       "Marine",    "Pending",  7800000.00m, "THB", "2027-05-01", "2028-05-01", "Thailand",    "Krit Wongchai",   false),
            ("POL-000140", "Genting Group Malaysia",             "Property",  "Pending",  1600000.00m, "USD", "2027-03-01", "2028-03-01", "Malaysia",    "Faridah Malik",   false),

            // Long-expired policies (pre-2022)
            ("POL-000141", "Neptune Orient Lines",               "Marine",    "Expired",   540000.00m, "SGD", "2019-01-01", "2020-01-01", "Singapore",   "Jason Koh",       false),
            ("POL-000142", "Wheelock Properties HK",             "Property",  "Expired",  2800000.00m, "HKD", "2019-06-01", "2020-06-01", "Hong Kong",   "David Cheung",    false),
            ("POL-000143", "Ansell Ltd Australia",               "Casualty",  "Expired",   340000.00m, "AUD", "2020-03-01", "2021-03-01", "Australia",   "Emma Collins",    false),
            ("POL-000144", "Nomura Holdings Japan",              "Casualty",  "Expired", 290000000.00m,"JPY", "2018-10-01", "2019-10-01", "Japan",       "Yuki Tanaka",     false),
            ("POL-000145", "Gulf Energy Thailand",               "Property",  "Expired",  4200000.00m, "THB", "2020-07-01", "2021-07-01", "Thailand",    "Nattapong Chai",  false),
            ("POL-000146", "Lippo Group Indonesia",              "Property",  "Expired",   490000.00m, "USD", "2019-11-01", "2020-11-01", "Indonesia",   "Budi Santoso",    false),
            ("POL-000147", "Gamuda Bhd Malaysia",                "Property",  "Expired",  1900000.00m, "MYR", "2020-05-01", "2021-05-01", "Malaysia",    "Ahmad Razali",    false),
            ("POL-000148", "JG Summit Philippines",              "Casualty",  "Expired",   610000.00m, "USD", "2019-08-01", "2020-08-01", "Philippines", "Ramon Cruz",      false),

            // Cancelled policies spread across regions
            ("POL-000149", "SingTel Group Operations",           "Casualty",  "Cancelled",  78000.00m, "SGD", "2024-06-01", "2025-06-01", "Singapore",   "Sarah Tan",       false),
            ("POL-000150", "HK Broadband Network",               "Casualty",  "Cancelled",  42000.00m, "HKD", "2024-09-01", "2025-09-01", "Hong Kong",   "Linda Ho",        false),
            ("POL-000151", "Crown Casino Melbourne",             "Casualty",  "Cancelled", 165000.00m, "AUD", "2024-02-01", "2025-02-01", "Australia",   "Tom Bradley",     false),
            ("POL-000152", "Softbank Group Japan",               "Casualty",  "Cancelled", 850000.00m, "USD", "2024-05-01", "2025-05-01", "Japan",       "Yuki Tanaka",     false),
            ("POL-000153", "TrueMove H Thailand",                "Casualty",  "Cancelled",  95000.00m, "THB", "2024-03-01", "2025-03-01", "Thailand",    "Siriporn Kham",   false),
            ("POL-000154", "Indosat Ooredoo Hutchison",          "Casualty",  "Cancelled", 120000.00m, "USD", "2024-07-01", "2025-07-01", "Indonesia",   "Rizky Pratama",   false),
            ("POL-000155", "Astro Malaysia Holdings",            "Casualty",  "Cancelled",  56000.00m, "MYR", "2024-04-01", "2025-04-01", "Malaysia",    "Zulkifli Hassan", false),
            ("POL-000156", "Cebu Pacific Air Philippines",       "Casualty",  "Cancelled", 230000.00m, "USD", "2024-08-01", "2025-08-01", "Philippines", "Maria Santos",    false),

            // High-premium A&H policies
            ("POL-000157", "Temasek Holdings Benefits",          "A&H",       "Active",   4800000.00m, "USD", "2025-01-01", "2026-01-01", "Singapore",   "Rachel Lee",      true),
            ("POL-000158", "Li Ka-shing Foundation HK",          "A&H",       "Active",   4500000.00m, "HKD", "2024-11-01", "2025-11-01", "Hong Kong",   "Victor Lam",      false),
            ("POL-000159", "BHP Employee Wellness AU",           "A&H",       "Active",   3900000.00m, "AUD", "2025-03-01", "2026-03-01", "Australia",   "Sophie Martin",   false),
            ("POL-000160", "Toyota Employee Trust JP",           "A&H",       "Active",  410000000.00m,"JPY", "2025-01-01", "2026-01-01", "Japan",       "Mika Suzuki",     false),

            // Low-premium small business policies
            ("POL-000161", "Changi Florists Pte Ltd",            "Property",  "Active",      1200.00m, "SGD", "2025-06-01", "2026-06-01", "Singapore",   "Sarah Tan",       false),
            ("POL-000162", "Wan Chai Noodle Shop HK",            "Casualty",  "Active",      2500.00m, "HKD", "2025-04-01", "2026-04-01", "Hong Kong",   "Linda Ho",        false),
            ("POL-000163", "Surry Hills Cafe AU",                "Property",  "Active",      3800.00m, "AUD", "2025-05-01", "2026-05-01", "Australia",   "Emma Collins",    false),
            ("POL-000164", "Shinjuku Ramen Bar JP",              "Property",  "Active",   1200000.00m, "JPY", "2025-02-01", "2026-02-01", "Japan",       "Kenji Yamamoto",  false),
            ("POL-000165", "Sukhumvit Street Food TH",           "Casualty",  "Active",     18000.00m, "THB", "2025-06-01", "2026-06-01", "Thailand",    "Nattapong Chai",  false),
            ("POL-000166", "Bali Boutique Resort ID",            "Property",  "Active",     45000.00m, "USD", "2025-04-01", "2026-04-01", "Indonesia",   "Dewi Kusuma",     false),
            ("POL-000167", "Petaling Jaya Bakery MY",            "Property",  "Active",     22000.00m, "MYR", "2025-05-01", "2026-05-01", "Malaysia",    "Ahmad Razali",    false),
            ("POL-000168", "Makati Tailoring Shop PH",           "Casualty",  "Active",     15000.00m, "USD", "2025-03-01", "2026-03-01", "Philippines", "Jose Reyes",      false),

            // More diverse Active policies  — spread coverage
            ("POL-000169", "GrabTaxi Holdings SG",               "Casualty",  "Active",    870000.00m, "USD", "2025-01-15", "2026-01-15", "Singapore",   "Michael Chan",    false),
            ("POL-000170", "Sea Ltd Singapore",                   "Casualty",  "Active",   1300000.00m, "USD", "2024-10-15", "2025-10-15", "Singapore",   "Sarah Tan",       false),
            ("POL-000171", "Alibaba HK Operations",              "Property",  "Active",   2100000.00m, "HKD", "2025-02-15", "2026-02-15", "Hong Kong",   "David Cheung",    false),
            ("POL-000172", "Tencent HK Holdings",                "Casualty",  "Active",   3400000.00m, "HKD", "2024-09-15", "2025-09-15", "Hong Kong",   "Linda Ho",        true),
            ("POL-000173", "NetEase HK Marine",                  "Marine",    "Active",    450000.00m, "HKD", "2025-06-01", "2026-06-01", "Hong Kong",   "James Yip",       false),
            ("POL-000174", "Fortescue Metals AU",                "Property",  "Active",   4100000.00m, "AUD", "2025-01-01", "2026-01-01", "Australia",   "Tom Bradley",     false),
            ("POL-000175", "Santos Energy Australia",            "Marine",    "Active",   2700000.00m, "AUD", "2024-11-15", "2025-11-15", "Australia",   "Adam Fisher",     false),
            ("POL-000176", "Itochu Corporation Japan",           "Marine",    "Active",  290000000.00m,"JPY", "2025-05-01", "2026-05-01", "Japan",       "Takeshi Ito",     false),
            ("POL-000177", "SoftBank Robotics Japan",            "Property",  "Active",  155000000.00m,"JPY", "2025-03-01", "2026-03-01", "Japan",       "Yuki Tanaka",     false),
            ("POL-000178", "Minor International TH",             "Property",  "Active",   8200000.00m, "THB", "2025-04-01", "2026-04-01", "Thailand",    "Siriporn Kham",   false),
            ("POL-000179", "Charoen Pokphand Group",             "Marine",    "Active",  14000000.00m, "THB", "2024-12-01", "2025-12-01", "Thailand",    "Krit Wongchai",   false),
            ("POL-000180", "Kalbe Farma Indonesia",              "A&H",       "Active",    390000.00m, "USD", "2025-05-01", "2026-05-01", "Indonesia",   "Rizky Pratama",   false),
            ("POL-000181", "Semen Indonesia Group",              "Property",  "Active",    510000.00m, "USD", "2024-10-01", "2025-10-01", "Indonesia",   "Budi Santoso",    false),
            ("POL-000182", "Axiata Group Malaysia",              "Casualty",  "Active",   1300000.00m, "MYR", "2025-02-01", "2026-02-01", "Malaysia",    "Zulkifli Hassan", false),
            ("POL-000183", "Sime Darby Plantation",              "Marine",    "Active",   1700000.00m, "MYR", "2024-09-01", "2025-09-01", "Malaysia",    "Rahim Nordin",    false),
            ("POL-000184", "Alliance Global PH",                 "Property",  "Active",   2300000.00m, "USD", "2025-06-01", "2026-06-01", "Philippines", "Maria Santos",    false),
            ("POL-000185", "Aboitiz Power Philippines",          "Casualty",  "Active",   1900000.00m, "USD", "2025-01-15", "2026-01-15", "Philippines", "Ramon Cruz",      false),

            // Extra Expired — more historical coverage
            ("POL-000186", "SIA Engineering Singapore",          "Casualty",  "Expired",   830000.00m, "SGD", "2021-01-01", "2022-01-01", "Singapore",   "Sarah Tan",       false),
            ("POL-000187", "Hopewell Holdings HK",               "Property",  "Expired",  1600000.00m, "HKD", "2021-04-01", "2022-04-01", "Hong Kong",   "David Cheung",    false),
            ("POL-000188", "Macquarie Group AU",                 "Casualty",  "Expired",   970000.00m, "AUD", "2021-07-01", "2022-07-01", "Australia",   "Tom Bradley",     false),
            ("POL-000189", "Suzuki Motor Japan",                 "Casualty",  "Expired", 210000000.00m,"JPY", "2021-10-01", "2022-10-01", "Japan",       "Hiroshi Sato",    false),
            ("POL-000190", "IRPC Plc Thailand",                  "Marine",    "Expired",  3800000.00m, "THB", "2021-06-01", "2022-06-01", "Thailand",    "Krit Wongchai",   false),
            ("POL-000191", "Bank Rakyat Indonesia",              "Casualty",  "Expired",   410000.00m, "USD", "2021-09-01", "2022-09-01", "Indonesia",   "Rizky Pratama",   false),
            ("POL-000192", "Hartalega Holdings MY",              "A&H",       "Expired",   290000.00m, "MYR", "2021-03-01", "2022-03-01", "Malaysia",    "Faridah Malik",   false),
            ("POL-000193", "Metro Pacific PH",                   "Property",  "Expired",   560000.00m, "USD", "2021-12-01", "2022-12-01", "Philippines", "Maria Santos",    false),

            // Extra Pending across all LoB
            ("POL-000194", "Singtel Next Gen SG",                "Marine",    "Pending",  1200000.00m, "SGD", "2026-12-01", "2027-12-01", "Singapore",   "Jason Koh",       false),
            ("POL-000195", "BOC Aviation HK",                    "Marine",    "Pending",  4700000.00m, "USD", "2026-11-01", "2027-11-01", "Hong Kong",   "James Yip",       false),
            ("POL-000196", "ResMed Inc Australia",               "A&H",       "Pending",   560000.00m, "AUD", "2026-12-01", "2027-12-01", "Australia",   "Sophie Martin",   false),
            ("POL-000197", "Nidec Corporation Japan",            "Property",  "Pending", 195000000.00m,"JPY", "2026-10-01", "2027-10-01", "Japan",       "Kenji Yamamoto",  false),
            ("POL-000198", "Central Group Thailand",             "A&H",       "Pending",  5600000.00m, "THB", "2026-11-01", "2027-11-01", "Thailand",    "Siriporn Kham",   false),
            ("POL-000199", "GoTo Group Indonesia",               "Casualty",  "Pending",   780000.00m, "USD", "2026-12-01", "2027-12-01", "Indonesia",   "Rizky Pratama",   false),
            ("POL-000200", "Tenaga Solar Malaysia",              "Property",  "Pending",  1400000.00m, "MYR", "2026-10-01", "2027-10-01", "Malaysia",    "Ahmad Razali",    false),
            ("POL-000201", "Nickel Asia Philippines",            "Marine",    "Pending",   920000.00m, "USD", "2026-11-01", "2027-11-01", "Philippines", "Leo Navarro",     false),

            // Flagged-for-review policies — spread across regions/LoB for filter testing
            ("POL-000202", "Genting Singapore Plc",              "Property",  "Active",   2900000.00m, "SGD", "2025-01-01", "2026-01-01", "Singapore",   "James Wong",      true),
            ("POL-000203", "Great Eagle Holdings HK",            "Property",  "Active",   3700000.00m, "HKD", "2024-12-01", "2025-12-01", "Hong Kong",   "David Cheung",    true),
            ("POL-000204", "Incitec Pivot Australia",            "Marine",    "Active",   1800000.00m, "AUD", "2025-05-01", "2026-05-01", "Australia",   "Adam Fisher",     true),
            ("POL-000205", "Fujitsu Limited Japan",              "A&H",       "Active",  240000000.00m,"JPY", "2025-04-01", "2026-04-01", "Japan",       "Mika Suzuki",     true),
            ("POL-000206", "True Corporation Thailand",          "Marine",    "Active",   6400000.00m, "THB", "2025-02-01", "2026-02-01", "Thailand",    "Krit Wongchai",   true),
            ("POL-000207", "Astra Otoparts Indonesia",           "Property",  "Active",    680000.00m, "USD", "2025-06-01", "2026-06-01", "Indonesia",   "Budi Santoso",    true),
            ("POL-000208", "RHB Bank Group Malaysia",            "A&H",       "Active",   1100000.00m, "MYR", "2025-03-01", "2026-03-01", "Malaysia",    "Faridah Malik",   true),
            ("POL-000209", "First Gen Corporation PH",           "Marine",    "Active",   2200000.00m, "USD", "2025-05-01", "2026-05-01", "Philippines", "Leo Navarro",     true),
            ("POL-000210", "Shopee Sea Group Singapore",         "Casualty",  "Active",   1700000.00m, "USD", "2025-06-01", "2026-06-01", "Singapore",   "Rachel Lee",      true),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: each INSERT only fires when PolicyNumber does not exist.
            // MigrationBuilder.Sql() uses raw SQL which is safe here because all
            // values are compile-time constants — no user input, no concatenation.
            foreach (var p in Seeds)
            {
                var id = Guid.NewGuid().ToString();
                var flagged = p.FlaggedForReview ? "1" : "0";
                var sql = $"""
                    IF NOT EXISTS (SELECT 1 FROM [Policies] WHERE [PolicyNumber] = '{p.PolicyNumber}')
                    BEGIN
                        INSERT INTO [Policies]
                            ([Id],[PolicyNumber],[PolicyholderName],[LineOfBusiness],[Status],
                             [PremiumAmount],[Currency],[EffectiveDate],[ExpiryDate],
                             [Region],[Underwriter],[FlaggedForReview],[CreatedAt],[UpdatedAt])
                        VALUES
                            ('{id}','{p.PolicyNumber}',N'{p.PolicyholderName}',
                             '{p.LineOfBusiness}','{p.Status}',
                             {p.PremiumAmount},{(char)39}{p.Currency}{(char)39},
                             '{p.EffectiveDate}','{p.ExpiryDate}',
                             '{p.Region}',N'{p.Underwriter}',
                             {flagged},'2026-01-01T00:00:00','2026-01-01T00:00:00')
                    END
                    """;
                migrationBuilder.Sql(sql);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seed data (safe because PolicyNumber values are unique constants)
            var numbers = string.Join(",", Seeds.Select(s => $"'{s.PolicyNumber}'"));
            migrationBuilder.Sql($"DELETE FROM [Policies] WHERE [PolicyNumber] IN ({numbers})");
        }
    }
}
