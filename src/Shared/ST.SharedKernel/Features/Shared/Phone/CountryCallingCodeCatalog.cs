namespace ST.SharedKernel.Features.Shared.Phone;

public static class CountryCallingCodeCatalog
{
    private static readonly CountryCallingCode[] Countries =
    [
        new("AF", "Afghanistan", "+93", 8, 9),
        new("AL", "Albania", "+355", 8, 9),
        new("DZ", "Algeria", "+213", 8, 9),
        new("AD", "Andorra", "+376", 6, 9),
        new("AO", "Angola", "+244", 9, 9),
        new("AR", "Argentina", "+54", 10, 10),
        new("AM", "Armenia", "+374", 8, 8),
        new("AU", "Australia", "+61", 9, 9),
        new("AT", "Austria", "+43", 7, 13),
        new("AZ", "Azerbaijan", "+994", 9, 9),
        new("BH", "Bahrain", "+973", 8, 8),
        new("BD", "Bangladesh", "+880", 10, 10),
        new("BY", "Belarus", "+375", 9, 9),
        new("BE", "Belgium", "+32", 8, 9),
        new("BZ", "Belize", "+501", 7, 7),
        new("BJ", "Benin", "+229", 8, 10),
        new("BT", "Bhutan", "+975", 7, 8),
        new("BO", "Bolivia", "+591", 8, 8),
        new("BA", "Bosnia and Herzegovina", "+387", 8, 8),
        new("BW", "Botswana", "+267", 7, 8),
        new("BR", "Brazil", "+55", 10, 11),
        new("BN", "Brunei", "+673", 7, 7),
        new("BG", "Bulgaria", "+359", 8, 9),
        new("BF", "Burkina Faso", "+226", 8, 8),
        new("BI", "Burundi", "+257", 8, 8),
        new("KH", "Cambodia", "+855", 8, 9),
        new("CM", "Cameroon", "+237", 9, 9),
        new("CA", "Canada", "+1", 10, 10),
        new("CV", "Cape Verde", "+238", 7, 7),
        new("CF", "Central African Republic", "+236", 8, 8),
        new("TD", "Chad", "+235", 8, 8),
        new("CL", "Chile", "+56", 9, 9),
        new("CN", "China", "+86", 11, 11),
        new("CO", "Colombia", "+57", 10, 10),
        new("KM", "Comoros", "+269", 7, 7),
        new("CD", "Congo, Democratic Republic", "+243", 9, 9),
        new("CG", "Congo, Republic", "+242", 9, 9),
        new("CR", "Costa Rica", "+506", 8, 8),
        new("CI", "Cote d'Ivoire", "+225", 8, 10),
        new("HR", "Croatia", "+385", 8, 9),
        new("CU", "Cuba", "+53", 8, 8),
        new("CY", "Cyprus", "+357", 8, 8),
        new("CZ", "Czechia", "+420", 9, 9),
        new("DK", "Denmark", "+45", 8, 8),
        new("DJ", "Djibouti", "+253", 8, 8),
        new("DO", "Dominican Republic", "+1", 10, 10),
        new("EC", "Ecuador", "+593", 8, 9),
        new("EG", "Egypt", "+20", 9, 10),
        new("SV", "El Salvador", "+503", 8, 8),
        new("GQ", "Equatorial Guinea", "+240", 9, 9),
        new("ER", "Eritrea", "+291", 7, 7),
        new("EE", "Estonia", "+372", 7, 10),
        new("SZ", "Eswatini", "+268", 8, 8),
        new("ET", "Ethiopia", "+251", 9, 9),
        new("FJ", "Fiji", "+679", 7, 7),
        new("FI", "Finland", "+358", 7, 12),
        new("FR", "France", "+33", 9, 9),
        new("GA", "Gabon", "+241", 7, 9),
        new("GM", "Gambia", "+220", 7, 7),
        new("GE", "Georgia", "+995", 9, 9),
        new("DE", "Germany", "+49", 7, 11),
        new("GH", "Ghana", "+233", 9, 9),
        new("GR", "Greece", "+30", 10, 10),
        new("GT", "Guatemala", "+502", 8, 8),
        new("GN", "Guinea", "+224", 9, 9),
        new("GW", "Guinea-Bissau", "+245", 7, 7),
        new("GY", "Guyana", "+592", 7, 7),
        new("HT", "Haiti", "+509", 8, 8),
        new("HN", "Honduras", "+504", 8, 8),
        new("HK", "Hong Kong", "+852", 8, 8),
        new("HU", "Hungary", "+36", 8, 9),
        new("IS", "Iceland", "+354", 7, 9),
        new("IN", "India", "+91", 10, 10),
        new("ID", "Indonesia", "+62", 9, 12),
        new("IR", "Iran", "+98", 10, 10),
        new("IQ", "Iraq", "+964", 10, 10),
        new("IE", "Ireland", "+353", 7, 10),
        new("IL", "Israel", "+972", 8, 9),
        new("IT", "Italy", "+39", 6, 11),
        new("JM", "Jamaica", "+1", 10, 10),
        new("JP", "Japan", "+81", 10, 10),
        new("JO", "Jordan", "+962", 9, 9),
        new("KZ", "Kazakhstan", "+7", 10, 10),
        new("KE", "Kenya", "+254", 9, 9),
        new("KW", "Kuwait", "+965", 8, 8),
        new("KG", "Kyrgyzstan", "+996", 9, 9),
        new("LA", "Laos", "+856", 8, 10),
        new("LV", "Latvia", "+371", 8, 8),
        new("LB", "Lebanon", "+961", 7, 8),
        new("LS", "Lesotho", "+266", 8, 8),
        new("LR", "Liberia", "+231", 7, 8),
        new("LY", "Libya", "+218", 8, 9),
        new("LT", "Lithuania", "+370", 8, 8),
        new("LU", "Luxembourg", "+352", 6, 11),
        new("MG", "Madagascar", "+261", 9, 9),
        new("MW", "Malawi", "+265", 8, 9),
        new("MY", "Malaysia", "+60", 8, 10),
        new("MV", "Maldives", "+960", 7, 7),
        new("ML", "Mali", "+223", 8, 8),
        new("MT", "Malta", "+356", 8, 8),
        new("MR", "Mauritania", "+222", 8, 8),
        new("MU", "Mauritius", "+230", 7, 8),
        new("MX", "Mexico", "+52", 10, 10),
        new("MD", "Moldova", "+373", 8, 8),
        new("MN", "Mongolia", "+976", 8, 8),
        new("ME", "Montenegro", "+382", 8, 8),
        new("MA", "Morocco", "+212", 9, 9),
        new("MZ", "Mozambique", "+258", 8, 9),
        new("MM", "Myanmar", "+95", 7, 10),
        new("NA", "Namibia", "+264", 8, 9),
        new("NP", "Nepal", "+977", 10, 10),
        new("NL", "Netherlands", "+31", 9, 9),
        new("NZ", "New Zealand", "+64", 8, 10),
        new("NI", "Nicaragua", "+505", 8, 8),
        new("NE", "Niger", "+227", 8, 8),
        new("NG", "Nigeria", "+234", 10, 10),
        new("NO", "Norway", "+47", 8, 8),
        new("OM", "Oman", "+968", 8, 8),
        new("PK", "Pakistan", "+92", 10, 10),
        new("PA", "Panama", "+507", 8, 8),
        new("PG", "Papua New Guinea", "+675", 7, 8),
        new("PY", "Paraguay", "+595", 8, 9),
        new("PE", "Peru", "+51", 8, 9),
        new("PH", "Philippines", "+63", 10, 10),
        new("PL", "Poland", "+48", 9, 9),
        new("PT", "Portugal", "+351", 9, 9),
        new("QA", "Qatar", "+974", 8, 8),
        new("RO", "Romania", "+40", 9, 9),
        new("RU", "Russia", "+7", 10, 10),
        new("RW", "Rwanda", "+250", 9, 9),
        new("SA", "Saudi Arabia", "+966", 9, 9),
        new("SN", "Senegal", "+221", 9, 9),
        new("RS", "Serbia", "+381", 8, 9),
        new("SC", "Seychelles", "+248", 7, 7),
        new("SL", "Sierra Leone", "+232", 8, 8),
        new("SG", "Singapore", "+65", 8, 8),
        new("SK", "Slovakia", "+421", 9, 9),
        new("SI", "Slovenia", "+386", 8, 8),
        new("SO", "Somalia", "+252", 7, 8),
        new("ZA", "South Africa", "+27", 9, 9),
        new("KR", "South Korea", "+82", 9, 10),
        new("SS", "South Sudan", "+211", 9, 9),
        new("ES", "Spain", "+34", 9, 9),
        new("LK", "Sri Lanka", "+94", 9, 9),
        new("SD", "Sudan", "+249", 9, 9),
        new("SE", "Sweden", "+46", 7, 10),
        new("CH", "Switzerland", "+41", 9, 9),
        new("SY", "Syria", "+963", 9, 9),
        new("TW", "Taiwan", "+886", 9, 9),
        new("TJ", "Tajikistan", "+992", 9, 9),
        new("TZ", "Tanzania", "+255", 9, 9),
        new("TH", "Thailand", "+66", 8, 9),
        new("TG", "Togo", "+228", 8, 8),
        new("TN", "Tunisia", "+216", 8, 8),
        new("TR", "Turkey", "+90", 10, 10),
        new("TM", "Turkmenistan", "+993", 8, 8),
        new("UG", "Uganda", "+256", 9, 9),
        new("UA", "Ukraine", "+380", 9, 9),
        new("AE", "United Arab Emirates", "+971", 8, 9),
        new("GB", "United Kingdom", "+44", 9, 10),
        new("US", "United States", "+1", 10, 10),
        new("UY", "Uruguay", "+598", 8, 8),
        new("UZ", "Uzbekistan", "+998", 9, 9),
        new("VE", "Venezuela", "+58", 10, 10),
        new("VN", "Vietnam", "+84", 9, 10),
        new("YE", "Yemen", "+967", 8, 9),
        new("ZM", "Zambia", "+260", 9, 9),
        new("ZW", "Zimbabwe", "+263", 9, 9)
    ];

    public static IReadOnlyList<CountryCallingCode> All => Countries;

    public static CountryCallingCode Default => Countries.Single(country => country.Iso2 == "KE");

    public static CountryCallingCode? FindByDialCode(string? dialCode)
    {
        if (string.IsNullOrWhiteSpace(dialCode))
        {
            return Default;
        }

        var normalized = NormalizeDialCode(dialCode);
        return Countries
            .Where(country => country.DialCode == normalized)
            .OrderBy(country => country.Iso2 == "KE" ? 0 : 1)
            .ThenBy(country => country.Name)
            .FirstOrDefault();
    }

    public static CountryCallingCode? FindByE164(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber) || !phoneNumber.StartsWith('+'))
        {
            return null;
        }

        return Countries
            .OrderByDescending(country => country.DialCode.Length)
            .FirstOrDefault(country => phoneNumber.StartsWith(country.DialCode, StringComparison.Ordinal));
    }

    public static IReadOnlyList<CountryCallingCode> Search(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Countries
                .OrderBy(country => country.Iso2 == "KE" ? 0 : 1)
                .ThenBy(country => country.Name)
                .ToList();
        }

        var search = searchText.Trim();
        return Countries
            .Where(country =>
                country.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || country.Iso2.Contains(search, StringComparison.OrdinalIgnoreCase)
                || country.DialCode.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(country => country.Name)
            .ToList();
    }

    public static bool IsSupported(string? dialCode)
        => FindByDialCode(dialCode) is not null;

    public static string GetFlagEmoji(string iso2)
    {
        ArgumentNullException.ThrowIfNull(iso2);

        if (iso2.Length != 2)
        {
            return string.Empty;
        }

        const int regionalIndicatorOffset = 0x1F1E6 - 'A';
        return string.Concat(iso2.ToUpperInvariant().Select(ch => char.ConvertFromUtf32(ch + regionalIndicatorOffset)));
    }

    private static string NormalizeDialCode(string dialCode)
    {
        var digits = new string(dialCode.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? Default.DialCode : $"+{digits}";
    }
}
