using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using MongoDB.Driver;
using System.Text;

namespace HNTAS.Core.Api.Services
{
    public class CsvImportService : ICsvImportService
    {
        private readonly IOrganisationService _organisationService;
        private readonly IUserService _userService;
        private readonly IHeatNetworkService _heatNetworkService;
        private readonly ILogger<CsvImportService> _logger;

        public CsvImportService(
            IOrganisationService organisationService,
            IUserService userService,
            IHeatNetworkService heatNetworkService,
            ILogger<CsvImportService> logger)
        {
            _organisationService = organisationService;
            _userService = userService;
            _heatNetworkService = heatNetworkService;
            _logger = logger;
        }

        public async Task<ImportResult> ImportFromCsvAsync(string fileContent, CancellationToken ct = default)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            stream.Position = 0;

            var result = new ImportResult();

            var csvParser = new CsvParser();

            if (!csvParser.TryParseHeaders(stream, out var headerIndex, out var error))
            {
                result.Errors.Add(error);
                return result;
            }

            int lineNumber = 1;

            var ofgemDataModelPostImportList = new List<OfgemDataModelPostImport>();

            await foreach (var line in csvParser.ReadLinesAsync(stream, ct))
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var row = ParseCsvRow(line, headerIndex);

                    if (!ValidateRow(row, lineNumber, result))
                    {
                        continue;
                    }

                    result.RowsProcessed++;

                    await ProcessNetworkCreationThroughOrgOrUserExistance(row, result, ofgemDataModelPostImportList, ct);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing CSV line {Line}", lineNumber);
                    result.Errors.Add($"Line {lineNumber}: {ex.Message}");
                }
            }

            var dataForExistingOrgOrUser = ofgemDataModelPostImportList
                .Where(x => x.IsUserOrOrganisationExist)
                .GroupBy(x => x.UserEmailId)
                .Select(g => new OfgemDataModelForNotification
                {
                    UserEmailId = g.Key,
                    OrganisationId = g.First().OrganisationId,
                    OrganisationName = g.First().OrganisationName,
                    HeatNetworkIds = g.Select(x => x.HeatNetworkId).ToList()
                }).ToList();

            var dataForNewOrgOrUser = ofgemDataModelPostImportList
                .Where(x => !x.IsUserOrOrganisationExist)
                .GroupBy(x => x.UserEmailId)
                .Select(g => new OfgemDataModelForNotification
                {
                    UserEmailId = g.Key,
                    OrganisationId = string.Empty,
                    OrganisationName = g.First().OrganisationName,
                    HeatNetworkIds = g.Select(x => x.HeatNetworkId).ToList()
                }).ToList();

            result.DataForExistingOrgOrUser = dataForExistingOrgOrUser;
            result.DataForNewOrgOrUser = dataForNewOrgOrUser;

            return result;
        }

        private CsvRow ParseCsvRow(string line, Dictionary<string, int> headerIndex)
        {
            var cells = CsvParser.SplitCsvLine(line);

            return new CsvRow
            {
                EmailId = CsvParser.GetCell(cells, headerIndex, "EmailId"),
                OneLoginId = CsvParser.GetCell(cells, headerIndex, "OneLoginId"),
                OrganisationName = CsvParser.GetCell(cells, headerIndex, "OrganisationName"),
                OrgStreetAddress = CsvParser.GetCell(cells, headerIndex, "OrgStreetAddress"),
                OrgTown = CsvParser.GetCell(cells, headerIndex, "OrgTown"),
                OrgPostcode = CsvParser.GetCell(cells, headerIndex, "OrgPostcode"),
                PhoneNumber = CsvParser.GetCell(cells, headerIndex, "PhoneNumber"),
                CompaniesHouseNo = CsvParser.GetCell(cells, headerIndex, "CompaniesHouseNo"),
                DateOfOrgRegistration = CsvParser.GetCell(cells, headerIndex, "DateOfRegistration"),
                HnId = CsvParser.GetCell(cells, headerIndex, "HnId"),
                HnName = CsvParser.GetCell(cells, headerIndex, "HnName"),
                DateOfHnRegistration = CsvParser.GetCell(cells, headerIndex, "DateOfHnRegistration"),
                EcStreetAddress = CsvParser.GetCell(cells, headerIndex, "EcStreetAddress"),
                EcTown = CsvParser.GetCell(cells, headerIndex, "EcTown"),
                EcPostcode = CsvParser.GetCell(cells, headerIndex, "EcPostcode"),
                EcLatitude = CsvParser.GetCell(cells, headerIndex, "ECLatitude"),
                EcLongitude = CsvParser.GetCell(cells, headerIndex, "ECLongitude")
            };
        }

        private bool ValidateRow(CsvRow row, int lineNumber, ImportResult result)
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(row.EmailId)) missingFields.Add("EmailId");
            if (string.IsNullOrWhiteSpace(row.HnName)) missingFields.Add("HnName");
            if (string.IsNullOrWhiteSpace(row.HnId)) missingFields.Add("HnId");

            var hasOrgIdentifier = !string.IsNullOrWhiteSpace(row.CompaniesHouseNo);
            if (!hasOrgIdentifier)
            {
                missingFields.Add("Organisation details (CompaniesHouseNo or Name+Address+Postcode)");
            }

            if (missingFields.Any())
            {
                result.Errors.Add($"Line {lineNumber}: Missing required fields: {string.Join(", ", missingFields)}");
                return false;
            }

            return true;
        }

        private async Task ProcessNetworkCreationThroughOrgOrUserExistance(
            CsvRow row,
            ImportResult result,
            List<OfgemDataModelPostImport> ofgemDataModelPostImportList,
            CancellationToken ct)
        {

            // Check organisation type based on presence of CompaniesHouseNo
            var orgType = !string.IsNullOrWhiteSpace(row.CompaniesHouseNo)
                ? Enums.OrganisationType.UkCompaniesHouse
                : Enums.OrganisationType.OtherUkOrganisation;

            Organisation? orgDetails = null;
            if (orgType == Enums.OrganisationType.UkCompaniesHouse)
            {
                // find if the associated OrgId is based on CompanyHouseNumber
                orgDetails = await _organisationService.GetByCompanyHouseNumberAsync(row.CompaniesHouseNo);
                var userDetails = await _userService.GetByEmailAsync(row.EmailId.ToLower());

                // Org exists
                if (orgDetails != null)
                {
                    // Create a network and link to the org
                    await ProcessHeatNetworkAsync(row, result, ofgemDataModelPostImportList, userDetails.Id!, orgDetails.OrgId!, orgDetails.Name, ct);
                }
                else
                {
                    await ProcessNetworkCreationThroughUserExistance(row, result, ofgemDataModelPostImportList, ct);
                }
            }
            else
            {
                await ProcessNetworkCreationThroughUserExistance(row, result, ofgemDataModelPostImportList, ct);
            }
        }

        private async Task ProcessNetworkCreationThroughUserExistance(CsvRow row,
            ImportResult result,
            List<OfgemDataModelPostImport> ofgemDataModelPostImportList,
            CancellationToken ct)
        {
            // Check with user id
            var user = await _userService.GetByEmailAsync(row.EmailId.ToLower());
            // User exists and has RP role
            if (user != null && user.Roles.Contains(Enums.UserRole.ResponsibleParty))
            {
                // Get org details for the user
                var userOrgDetails = await _organisationService.GetByOrgIdAsync(user.OrgId!);
                // Create a network and link to the user and org
                await ProcessHeatNetworkAsync(row, result, ofgemDataModelPostImportList, user.Id!, user.OrgId!, userOrgDetails.Name, ct);
            }
            else
            {
                var existingHn = await _heatNetworkService.GetByHnIdAsync(row.HnId);

                if (existingHn != null)
                {
                    _logger.LogInformation("HeatNetwork {HnId} already exists.", row.HnId);
                    return;
                }
                // Send email to user to reg their org and heat network
                var ofgemDataModelPostImport = new OfgemDataModelPostImport
                {
                    HeatNetworkId = row.HnId!,
                    OrganisationId = string.Empty,
                    OrganisationName = row.OrganisationName,
                    UserId = string.Empty,
                    UserEmailId = row.EmailId.ToLower(),
                    IsUserOrOrganisationExist = false
                };

                ofgemDataModelPostImportList.Add(ofgemDataModelPostImport);

                await CreateHeatNetwork(row, null, null);
                result.HeatNetworksInserted++;
            }
        }
        private async Task ProcessHeatNetworkAsync(
            CsvRow row,
            ImportResult result,
            List<OfgemDataModelPostImport> ofgemDataModelPostImportList,
            string userId,
            string hntasOrgId,
            string hntasOrgName,
            CancellationToken ct)
        {
            var existingHn = await _heatNetworkService.GetByHnIdAsync(row.HnId);

            if (existingHn != null)
            {
                _logger.LogInformation("HeatNetwork {HnId} already exists.", row.HnId);
                return;
            }

            await CreateHeatNetwork(row, hntasOrgId, userId);
            await _organisationService.UpdateAsync(hntasOrgId, row.HnId);
            await _userService.UpdateUserNetwork(userId, row.HnId);

            var ofgemDataModelPostImport = new OfgemDataModelPostImport
            {
                HeatNetworkId = row.HnId!,
                OrganisationId = hntasOrgId,
                OrganisationName = hntasOrgName,
                UserId = userId,
                UserEmailId = row.EmailId.ToLower(),
                IsUserOrOrganisationExist = true
            };

            ofgemDataModelPostImportList.Add(ofgemDataModelPostImport);
            result.HeatNetworksInserted++;
            _logger.LogInformation("Created HeatNetwork {HnId}.", row.HnId);
        }

        private async Task CreateHeatNetwork(CsvRow row, string hntasOrgId, string userId)
        {
            var newHn = new HeatNetwork
            {
                HnId = row.HnId,
                UHnId = row.HnId.Replace("HN", ""),
                OrgId = hntasOrgId,
                Name = row.HnName,
                Phase = "Operation",
                HasAddressAndPostcode = row.EcPostcode != string.Empty ? true : false,
                Address = new RegisteredAddress
                {
                    AddressLine1 = row.EcStreetAddress ?? string.Empty,
                    Town = row.EcTown ?? string.Empty,
                    Postcode = row.EcPostcode ?? string.Empty,
                    Country = "United Kingdom"
                },
                ECDetails = new ECDetails
                {
                    Latitude = ParseDecimal(row.EcLatitude),
                    Longitude = ParseDecimal(row.EcLongitude)
                },
                HeatNetworkType = Enums.HeatNetworkType.Unset,
                RegistrationSource = Enums.RegistrationSource.OFGEM,
                OfgemUserEmailId = string.IsNullOrEmpty(userId) ? row.EmailId.ToLower() : null,
                CreatedBy = userId,
                CreatedAt = ParseDate(row.DateOfHnRegistration),
                OfgemImportedDate = DateTime.UtcNow
            };

            await _heatNetworkService.CreateAsync(newHn);
        }

        private static DateTime ParseDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                return DateTime.UtcNow;

            return DateTime.ParseExact(
                dateString,
                new[] { "dd/MM/yyyy", "d/M/yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal);
        }

        private static decimal? ParseDecimal(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }        
    }

    // Supporting classes
    internal class CsvRow
    {
        public string EmailId { get; set; } = string.Empty;
        public string OneLoginId { get; set; } = string.Empty;
        public string OrganisationName { get; set; } = string.Empty;
        public string OrgStreetAddress { get; set; } = string.Empty;
        public string OrgTown { get; set; } = string.Empty;
        public string OrgPostcode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CompaniesHouseNo { get; set; } = string.Empty;
        public string DateOfOrgRegistration { get; set; } = string.Empty;
        public string HnId { get; set; } = string.Empty;
        public string HnName { get; set; } = string.Empty;
        public string DateOfHnRegistration { get; set; } = string.Empty;
        public string EcStreetAddress { get; set; } = string.Empty;
        public string EcTown { get; set; } = string.Empty;
        public string EcPostcode { get; set; } = string.Empty;
        public string EcLatitude { get; set; } = string.Empty;
        public string EcLongitude { get; set; } = string.Empty;
    }

    internal class CsvParser
    {
        public bool TryParseHeaders(Stream stream, out Dictionary<string, int> headerIndex, out string error)
        {
            headerIndex = new Dictionary<string, int>();
            error = string.Empty;

            using var reader = new StreamReader(stream, leaveOpen: true);
            var headerLine = reader.ReadLine();

            if (string.IsNullOrWhiteSpace(headerLine))
            {
                error = "CSV is missing a header row.";
                return false;
            }

            var headers = SplitCsvLine(headerLine);
            headerIndex = headers
                .Select((h, i) => new { h, i })
                .ToDictionary(x => x.h.Trim(), x => x.i, StringComparer.OrdinalIgnoreCase);

            stream.Position = 0; // Reset for reading lines
            return true;
        }

        public async IAsyncEnumerable<string> ReadLinesAsync(Stream stream, CancellationToken ct)
        {
            using var reader = new StreamReader(stream);
            await reader.ReadLineAsync(); // Skip header

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (ct.IsCancellationRequested)
                    yield break;

                yield return line;
            }
        }

        public static string[] SplitCsvLine(string line)
        {
            var values = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ',' && !inQuotes)
                {
                    values.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            values.Add(sb.ToString());
            return values.ToArray();
        }

        public static string GetCell(string[] cells, Dictionary<string, int> headerIndex, string col)
        {
            return headerIndex.TryGetValue(col, out int index) && index < cells.Length
                ? cells[index]
                : string.Empty;
        }
    }

    public class OfgemDataModelPostImport
    {
        public string HeatNetworkId { get; set; } = string.Empty;
        public string OrganisationId { get; set; } = string.Empty;
        public string OrganisationName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserEmailId { get; set; } = string.Empty;
        public bool IsUserOrOrganisationExist { get; set; }
    }

    public class OfgemDataModelForNotification
    {
        public List<string> HeatNetworkIds { get; set; }
        public string OrganisationId { get; set; } = string.Empty;
        public string OrganisationName { get; set; } = string.Empty;
        public string UserEmailId { get; set; } = string.Empty;
    }
}