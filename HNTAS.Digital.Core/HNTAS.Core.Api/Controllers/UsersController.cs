using AutoMapper;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Extensions;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IOrganisationService _organisationService;
    private readonly IInvitationService _invitationService;
    private readonly ILogger<UsersController> _logger;
    private readonly ICounterService _orgCounterService;
    private readonly IMapper _mapper;
    private readonly IEmailService _emailService;
    private readonly IHeatNetworkService _heatNetworkService;


    public UsersController(IUserService userService,
                           IOrganisationService organizationService,
                           IInvitationService invitationService,
                           ILogger<UsersController> logger,
                           ICounterService orgCounterService,
                           IMapper mapper,
                           IEmailService emailService,
                           IHeatNetworkService heatNetworkService,
                           IAuditService auditService,
                           INotificationHistoryService notificationHistoryService)
    {
        _userService = userService;
        _organisationService = organizationService;
        _invitationService = invitationService;
        _logger = logger;
        _emailService = emailService;
        _orgCounterService = orgCounterService;
        _heatNetworkService = heatNetworkService;
        _mapper = mapper;
    }

    /// <summary>
    /// Retrieves a list of all users.
    /// </summary>
    /// <returns>A list of user objects.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserResponse>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<UserResponse>>> GetUsers()
    {
        _logger.LogInformation("Attempting to retrieve all users.");
        try
        {
            var users = await _userService.GetAsync();
            var userResponseList = _mapper.Map<List<UserResponse>>(users);
            _logger.LogInformation("Successfully retrieved {UserCount} users.", users.Count);
            return Ok(userResponseList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all users.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving users.");
        }
    }

    /// <summary>
    /// Retrieves a list of all users.
    /// </summary>
    /// <returns>A list of user objects.</returns>
    [HttpGet("user-details-by-id")]
    [ProducesResponseType(typeof(UserDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDetailsResponse))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDetailsResponse>> GetUsersDetails(string id)
    {
        _logger.LogInformation("Attempting to retrieve all users.");
        try
        {
            var userDetails = await _userService.GetUserWithDetailsAsync(id);

            var userResponse = _mapper.Map<UserDetailsResponse>(userDetails);

            //Manual mapping needed because of the complexity
            userResponse.HeatNetworks = UserNetworkHelper.GetAuthorizedNetworks(userDetails);

            _logger.LogInformation("Successfully retrieved {UserCount} users.", userResponse?.Id);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all users.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving users.");
        }
    }


    // <summary>
    /// Get a User by their ID
    /// </summary>
    /// <remarks>
    /// Retrieves a single user profile from the database using their unique ID.
    /// This endpoint is used to fetch the complete details of an existing user.
    /// </remarks>
    /// <param name="id">The unique ID (24-character hexadecimal string) of the user to retrieve.</param>
    /// <returns>
    /// A <see cref="StatusCodes.Status200OK"/> (OK) response with the found user object,
    /// or a <see cref="StatusCodes.Status404NotFound"/> (Not Found) response if no user matches the provided ID.
    /// </returns>
    [HttpGet("{id:length(24)}", Name = "GetUserById")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetById(string id)
    {
        _logger.LogInformation("Attempting to retrieve user with ID: {Id}", id.ToSafeLog());
        try
        {
            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found.", id.ToSafeLog());
                return NotFound();
            }
            _logger.LogInformation("Successfully retrieved user with ID: {Id}", id.ToSafeLog());
            var userResponse = _mapper.Map<UserResponse>(user);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating user ID format: {Id}", id.ToSafeLog());
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while validating the user ID.");
        }
    }

    /// <summary>
    /// Get a User by their email ID
    /// </summary>
    /// <param name="emailId">The email address of the user to retrieve.</param>
    /// <returns>
    /// A <see cref="StatusCodes.Status200OK"/> (OK) response with the found user object,
    /// or a <see cref="StatusCodes.Status400BadRequest"/> (Bad Request) if email is invalid,
    /// or a <see cref="StatusCodes.Status404NotFound"/> (Not Found) if no user matches the provided email.
    /// </returns>
    [HttpGet("email/{emailId}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetByEmail(string emailId)
    {
        if (string.IsNullOrWhiteSpace(emailId))
        {
            _logger.LogWarning("Get user by email request received with null or empty email ID.");
            return BadRequest("Email ID must be provided.");
        }

        _logger.LogInformation("Attempting to retrieve user with given email");
        try
        {
            var user = await _userService.GetByEmailAsync(emailId);

            if (user == null)
            {
                _logger.LogWarning("User with given email not found.");
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved user with given email");
            var userResponse = _mapper.Map<UserResponse>(user);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by given email");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the user.");
        }
    }


    /// <summary>
    /// Check if a user is a Responsible Party by their email ID
    /// </summary>
    /// <remarks>
    /// Validates whether the user associated with the given email ID has the RegulatoryContact role.
    /// </remarks>
    /// <param name="emailId">The email ID of the user to check.</param>
    /// <returns>
    /// A <see cref="StatusCodes.Status200OK"/> response with a boolean indicating role membership,
    /// or a <see cref="StatusCodes.Status404NotFound"/> if the user is not found.
    /// </returns>
    [HttpGet("is-rp-user/{emailId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> IsRpUser(string emailId)
    {
        string userId = string.Empty;
        try
        {
            var user = await _userService.GetByEmailAsync(emailId);

            if (user == null)
            {
                return NotFound();
            }

            userId = user.Id; // Store user ID for logging in case of an error

            bool isRegulatoryContact = user.Roles.Contains(UserRole.ResponsibleParty);
            return Ok(isRegulatoryContact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking Responsible Party role for user Id : {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while checking the user's role.");
        }
    }


    [HttpGet("is-active-user/{emailId}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(bool), StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<bool>> IsActiveUser(string emailId)
    {
        if (string.IsNullOrWhiteSpace(emailId))
        {
            return BadRequest("Email ID must be provided.");
        }

        var user = await _userService.GetByEmailAsync(emailId);

        if (user == null)
        {
            // User not found, so not active
            return NotFound();
        }

        // Assuming user.Status is an enum or property indicating active state
        bool isActive = user.Status == UserStatus.Active;

        return Ok(isActive);
    }

    /// <summary>
    /// Get a User by their OneLogin ID
    /// </summary>
    /// <param name="oneLoginId"></param>
    /// <returns></returns>
    [HttpGet("onelogin/{oneLoginId}", Name = "GetUserByOneLoginId")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserResponse>> GetUserByOneLoginId(string oneLoginId)
    {
        _logger.LogInformation("Attempting to retrieve user with ID: {Id}", oneLoginId.ToSafeLog());

        try
        {
            var user = await _userService.GetByUserOneLoginIdAsync(oneLoginId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found.", oneLoginId.ToSafeLog());
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved user with ID: {Id}", oneLoginId.ToSafeLog());
            var userResponse = _mapper.Map<UserResponse>(user);
            return Ok(userResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by OneLogin ID: {Id}", oneLoginId.ToSafeLog());
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving the user.");
        }
    }
    

    /// <summary>
    /// Register initial user after login
    /// </summary>
    /// <remarks>Creates a new user entry with minimal details upon first login, setting status to pending org setup.</remarks>
    /// <param name="registrationData">The initial user registration data (UserId, EmailId).</param>
    /// <returns>A newly created user profile or an existing one if already registered.</returns>
    [HttpPost("initial-entry")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> InitialRegisterUser([FromBody] InitialUserRegistrationRequest registrationData)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid initial registration data for UserId: {UserId}. Errors: {Errors}",
                registrationData.OneLoginId.ToSafeLog(), string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return ValidationProblem(ModelState);
        }

        try
        {
            var existingUser = await _userService.GetByUserOneLoginIdAsync(registrationData.OneLoginId);

            if (existingUser != null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "User Already Exists",
                    Detail = $"A user with the provided UserId ({registrationData.OneLoginId.ToSafeLog()}) already exists."
                });
            }

            var newUser = new User
            {
                OneLoginId = registrationData.OneLoginId,
                EmailId = registrationData.EmailId,
                Status = registrationData.Status,
                CreatedAt = DateTime.UtcNow,
            };

            await _userService.CreateAsync(newUser);

            _logger.LogInformation("New user initially registered: {UserId} (DB Id: {Id})", newUser.OneLoginId.ToSafeLog(), newUser.Id);

            return StatusCode(StatusCodes.Status201Created, newUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during initial user registration for UserId: {UserId}", registrationData.OneLoginId.ToSafeLog());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred during initial user registration."
            });
        }
    }

    /// <summary>
    /// Update Organisation Details for a User
    /// </summary>
    [HttpPatch("{id:length(24)}/org-details")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> UpdateUserAndOrgDetails(string id, [FromBody] UpdateUserOrganisationRequest request)
    {
        // Contact details validation logic remains the same
        var (landline, extension, mobile) = ContactDetailsValidationHelper.GetValidatedContactDetails(
            request.PreferredContactType,
            request.LandlineNumber,
            request.ContactNumberExtension,
            request.MobileNumber,
            ModelState
        );

        request.LandlineNumber = landline;
        request.ContactNumberExtension = extension;
        request.MobileNumber = mobile;

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid organisation details update data for user ID: {UserId}. Errors: {Errors}",
                id.ToSafeLog(), string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return ValidationProblem(ModelState);
        }

        try
        {
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for organisation details update.", id.ToSafeLog());
                return NotFound();
            }

            // Create a new Organization document using the data from the request
            var newOrg = new Organisation
            {
                OrgId = $"ORG{await _orgCounterService.GetNextSequenceValue("orgId_sequence"):D7}",
                Type = request.Organisation.Type,
                CompaniesHouseNumber = request.Organisation.CompaniesHouseNumber,
                Name = request.Organisation.Name,
                RegisteredAddress = _mapper.Map<RegisteredAddress>(request.Organisation.RegisteredAddress),
                CreatedBy = existingUser.Id,
                CreatedAt = DateTime.UtcNow,
                RpUserId = existingUser.Id
            };

            await _organisationService.CreateAsync(newOrg); // Save the new organization to its collection

            // Update the existing User document to link to the new Organization
            existingUser.OrgId = newOrg.OrgId;
            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.JobTitle = request.JobTitle;
            existingUser.PreferredContactType = request.PreferredContactType;
            existingUser.LandlineNumber = request.LandlineNumber;
            existingUser.MobileNumber = request.MobileNumber;
            existingUser.ContactNumberExtension = request.ContactNumberExtension;
            existingUser.Status = UserStatus.Active; // Set status as active here

            if (existingUser.Roles == null)
            {
                existingUser.Roles = new List<UserRole>() { request.Role };
            }
            else if (!existingUser.Roles.Contains(request.Role))
            {
                existingUser.Roles.Add(request.Role);
            }

            await _userService.UpdateAsync(id, existingUser);

            // Ofgem Record processing
            var existingNetworks = await _heatNetworkService.GetByOfgemEmailIdAsync(existingUser.EmailId);
            if (existingNetworks.Any())
            {
                // update createdBy and orgId for each network; Update HnId in Organisation; Update HnId in User
                foreach (var network in existingNetworks)
                {
                    network.CreatedBy = existingUser.Id!;
                    network.OrgId = newOrg.OrgId;
                    await _heatNetworkService.UpdateAsync(network.HnId!, network);
                    await _organisationService.UpdateAsync(newOrg.OrgId, network.HnId!);
                    await _userService.UpdateUserNetwork(existingUser.Id!, network.HnId!);
                }
            }

            _logger.LogInformation("Organisation details and status updated for user {UserId}. Generated OrgId: {OrgId}", id.ToSafeLog(), newOrg.Id);

            await _emailService.TrySendOrgCreatedEmailAsync(existingUser, newOrg);

            return Ok(existingUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organisation details for user {UserId}", id.ToSafeLog());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating Organisation details."
            });
        }
    }


    /// <summary>
    /// Registers a new Organisation and links its generated OrgId to the specified User.
    /// </summary>
    /// <param name="userId">The ID of the user whose OrgId field will be updated.</param>
    /// <param name="request">The data to create the new Organisation record.</param>
    /// <returns>The newly created Organisation object.</returns>
    [HttpPost("register-org-and-link/{userId}")]
    [ProducesResponseType(typeof(Organisation), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Organisation>> RegisterOrganisationAndLinkUserAsync(
        string userId,
        OrganisationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(userId) || !ObjectId.TryParse(userId, out _))
        {
            return BadRequest("Invalid or missing UserId.");
        }

        try
        {
            // Check the user exists
            var existingUser = await _userService.GetByIdAsync(userId);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID '{UserId}' not found for organisation registration.", userId.ToSafeLog());
                return NotFound($"User with ID '{userId}' was not found. Organisation was not created.");
            }

            var newOrganisation = new Organisation
            {
                OrgId = $"ORG{await _orgCounterService.GetNextSequenceValue("orgId_sequence"):D7}",
                Name = request.Name,
                CompaniesHouseNumber = request.CompaniesHouseNumber,
                Type = request.Type,
                RegisteredAddress = _mapper.Map<RegisteredAddress>(request.RegisteredAddress),
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
            };

            
            if (existingUser.Roles.Any())
            {
                // check if existing user is Network Manager, if yes then get the invitor's id to RP user id in the org
                if (existingUser.Roles.Contains(UserRole.NetworkManager))
                {
                    var invitaions = await _invitationService.GetByInvitedEmailAsync(existingUser.EmailId);
                    if (invitaions != null)
                        newOrganisation.RpUserId = invitaions.InviterUserId;
                }
                else if (existingUser.Roles.Contains(UserRole.ResponsibleParty))
                {
                    newOrganisation.RpUserId = existingUser.Id;
                }                
            }

            await _organisationService.CreateAsync(newOrganisation);

            _logger.LogInformation("Organisation created successfully. New OrgId: {OrgId}", newOrganisation.OrgId);

            var updateResult = await _userService.UpdateOrgIdAsync(userId, newOrganisation.OrgId);

            if (updateResult.ModifiedCount == 0)
            {
                _logger.LogError("Failed to modify user {UserId} OrgId. Matched: {Matched}, Modified: {Modified}. Starting rollback.", userId.ToSafeLog(), updateResult.MatchedCount, updateResult.ModifiedCount);

                // Initiate Rollback: Delete the newly created Organisation
                await _organisationService.RemoveAsync(newOrganisation.Id);

                // Return a Server Error indicating the linking failed
                return StatusCode(StatusCodes.Status500InternalServerError, $"Organisation created but failed to link to user {userId.ToSafeLog()}. Rollback executed.");
            }

            _logger.LogInformation("User {UserId} successfully updated with OrgId: {OrgId}. Modified count: {Count}",
                userId.ToSafeLog(), newOrganisation.OrgId, updateResult.ModifiedCount);

            // Return the created organisation object with 201 status
            return StatusCode(StatusCodes.Status201Created, newOrganisation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create new organisation record.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                "An unexpected error occurred during registration and linking: " + ex.Message);
        }
    }

    /// <summary>
    /// Update User Details
    /// </summary>
    [HttpPatch("{id:length(24)}/user-details")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<User>> UpdateUserDetails(string id, [FromBody] UpdateUserDetailsRequest request)
    {
        // 1. Contact details validation logic remains the same
        var (landline, extension, mobile) = ContactDetailsValidationHelper.GetValidatedContactDetails(
            request.PreferredContactType,
            request.LandlineNumber,
            request.ContactNumberExtension,
            request.MobileNumber,
            ModelState
        );

        request.LandlineNumber = landline;
        request.ContactNumberExtension = extension;
        request.MobileNumber = mobile;

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid user details update data for user ID: {UserId}. Errors: {Errors}",
                id.ToSafeLog(), string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            return ValidationProblem(ModelState);
        }

        try
        {
            // 2. Find the existing user
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for user details update.", id.ToSafeLog());
                return NotFound();
            }

            // 3. Update only the user-specific fields
            existingUser.FirstName = request.FirstName;
            existingUser.LastName = request.LastName;
            existingUser.JobTitle = request.JobTitle;
            existingUser.PreferredContactType = request.PreferredContactType;
            existingUser.LandlineNumber = request.LandlineNumber;
            existingUser.MobileNumber = request.MobileNumber;
            existingUser.ContactNumberExtension = request.ContactNumberExtension;


            if (request.Role != null)
            {
                if (existingUser.Roles == null)
                {
                    existingUser.Roles = new List<UserRole>() { request.Role.Value };
                }
                else if (!existingUser.Roles.Contains(request.Role.Value))
                {
                    existingUser.Roles.Add(request.Role.Value);
                }
            }

            await _userService.UpdateAsync(id, existingUser);

            _logger.LogInformation("User details updated for user {UserId}.", id.ToSafeLog());

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user details for user {UserId}", id.ToSafeLog());
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while updating User details."
            });
        }
    }


    /// <summary>
    /// Gets list Contributor Roles from Enum class
    /// </summary>
    /// <returns>list Contributor Roles</returns>
    [HttpGet("contributor-roles")]
    [ProducesResponseType(typeof(List<EnumItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetContributorRoles()
    {
        var roles = EnumHelper.GetEnumItems<ContributorRole>();
        return Ok(roles);
    }


    /// <summary>
    /// Gets list Contributor Roles from Enum class
    /// </summary>
    /// <returns>list Contributor Roles</returns>
    [HttpGet("user-roles")]
    [ProducesResponseType(typeof(List<EnumItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetUserRoles()
    {
        var roles = EnumHelper.GetEnumItems<UserRole>();
        return Ok(roles);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteUser(string id)
    {
        _logger.LogInformation("Attempting to delete user with ID: {UserId}", id.ToSafeLog());
        var existingUser = await _userService.GetByIdAsync(id);
        if (existingUser == null)
        {
            _logger.LogWarning("Delete request for user ID: {UserId} failed. User not found.", id.ToSafeLog());
            return NotFound();
        }

        try
        {
            await _userService.RemoveAsync(id);
            _logger.LogInformation("User with ID: {UserId} successfully removed.", id.ToSafeLog());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting user with ID: {UserId}", id.ToSafeLog());
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the user.");
        }
    }


    [HttpGet("organisation/exists")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(bool))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<bool>> CheckOrganisationExistence(
        [FromQuery] string? companiesHouseNumber)
    {
        if (string.IsNullOrWhiteSpace(companiesHouseNumber))
        {
            _logger.LogWarning("Invalid request: 'companiesHouseNumber' query parameter is required.");
            return BadRequest("'companiesHouseNumber' must be provided.");
        }

        _logger.LogInformation("Checking if organisation with Companies House Number '{CompaniesHouseNumber}' has registered users.", StringFormatter.Sanitize(companiesHouseNumber));

        try
        {
            bool exists = await _organisationService.IsOrganizationExists(companiesHouseNumber);
            _logger.LogInformation("Organisation exists: {Exists}", exists);
            return Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking organisation existence.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }


    // TODO - Not updating because the feature shall be discarded soon. Either update or discard this method, once dhb-690 implemented
    #region managed-users delete/update
    [HttpGet("managed-users")]
    [ProducesResponseType(typeof(List<ManagedUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<List<ManagedUserResponse>>> GetManagedUsersAsync(string userId, bool networkManagersOnly = false)
    {
        var user = await _userService.GetUserWithDetailsAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", userId.ToSafeLog());
            return NotFound();
        }
        var managedUsers = new List<ManagedUserResponse>();

        // Get invitations and registered users
        var invitations = await _invitationService.GetInvitedUsersAsRegisteredAsync(user.Id);
        var invitedEmails = invitations.Select(i => i.EmailId).Distinct().ToList();
        var invitedUsersDetail = await _userService.GetUsersByInvitedEmailsWithDetailsAsync(invitedEmails);
        var registeredUsers = _mapper.Map<List<ManagedUserResponse>>(invitedUsersDetail);
        foreach (var ruser in registeredUsers)
        {
            var sourceUser = invitedUsersDetail.FirstOrDefault(x => x.Id == ruser.Id);
            ruser.HeatNetworks = MapHeatNetworks(sourceUser);
        }
        var coordinatorRoleName = UserRole.NetworkManager.ToString();
        if (registeredUsers != null && registeredUsers.Any())
        {
            // Exclude the responsible user
            registeredUsers = registeredUsers.Where(ru => ru.EmailId != user.EmailId).ToList();

            // If only network manager are required, include only registered users who have the NetworkManager role.
            if (networkManagersOnly)
            {
                registeredUsers = registeredUsers
                    .Where(ru => ru.Roles != null && ru.Roles.Any(r => string.Equals(r, coordinatorRoleName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            else
            {
                registeredUsers = registeredUsers
                    .Where(ru => ru.Roles == null || !ru.Roles.Any(r => string.Equals(r, coordinatorRoleName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            managedUsers.AddRange(registeredUsers);
        }

        // Process invited users (latest per email/HN combination)
        var invitedUsers = invitations
            .GroupBy(i => new { i.EmailId, i.HeatNetworks?.FirstOrDefault()?.HnId })
            .Select(g => g.OrderByDescending(i => i.InvitedAt).First())
            .Where(i =>
            {
                // Check if this specific person (Email + Network) is already registered
                bool isRegistered = registeredUsers.Any(u =>
                    u.EmailId == i.EmailId &&
                    u.HeatNetworks.Any(hn => hn.HnId == (i.HeatNetworks.FirstOrDefault()?.HnId))
                );

                bool isAccepted = i.Status == InvitationStatus.Accepted.ToString();

                // ONLY show the record if they are NOT registered
                // This will show "Invited" or "Rejected" status records
                return !isRegistered && !isAccepted;
            })
        .ToList();

        if (invitedUsers.Any())
        {
            if (networkManagersOnly)
            {
                invitedUsers = invitedUsers
                    .Where(ru => ru.Roles != null && ru.Roles.Any(r => string.Equals(r, coordinatorRoleName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            else
            {
                invitedUsers = invitedUsers
                    .Where(ru => ru.Roles == null || !ru.Roles.Any(r => string.Equals(r, coordinatorRoleName, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            managedUsers.AddRange(invitedUsers);
        }

        _logger.LogInformation("Managed users retrieved successfully for user ID: {UserId}", userId.ToSafeLog());
        return Ok(managedUsers);
    }
    #endregion

    [HttpGet("ddh-and-contributors-paginated")]
    [ProducesResponseType(typeof(PagedResult<ManagedUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<PagedResult<ManagedUserResponse>>> GetDdhAndContributorsPaginated(
        string userId,         
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "firstName",
        [FromQuery] string sortDirection = "asc")
    {
        var user = await _userService.GetUserWithDetailsAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", userId.ToSafeLog());
            return NotFound();
        }
        var managedUsers = new List<ManagedUserResponse>();
        
        var (invitations, totalCount) = await _invitationService.GetInvitedUsersDdhAndContributorsAsync(user.Id, pageNumber, pageSize, sortBy, sortDirection);
        
        // get the invitations where the status is accepted
        var acceptedInvitations = invitations.Where(i => i.Status == InvitationStatus.Accepted.ToString()).ToList();

        var usersStatusFromAcceptedInvitation = await _userService.GetActiveUsers(acceptedInvitations);

        foreach (var userStatus in usersStatusFromAcceptedInvitation)
        {
            var invitation = invitations.FirstOrDefault(i => i.EmailId == userStatus.EmailId && i.HeatNetworks!.Any(hn => hn.HnId == (userStatus.HeatNetworks!.FirstOrDefault()?.HnId)));
            if (invitation != null)
            {
                invitation.Status = userStatus.Status;
            }
        }

        _logger.LogInformation("Managed users retrieved successfully for user ID: {UserId}", userId.ToSafeLog());
        return Ok(new PagedResult<ManagedUserResponse>
        {
            Items = invitations,
            TotalCount =  (int)totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("network-managers")]
    [ProducesResponseType(typeof(List<InvitedUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<List<InvitedUserResponse>>> GetNetworkManagersAsync(string userId)
    {
        var user = await _userService.GetUserWithDetailsAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", StringFormatter.Sanitize(userId));
            return NotFound();
        }

        var networkManagerInvitations = await _invitationService.GetNetworkManagersByInviterUserId(userId);

        _logger.LogInformation("Successfully retrieved {Count} network manager invitations for user ID: {UserId}",
            networkManagerInvitations.Count, StringFormatter.Sanitize(userId));
        var managedNetworkMangers = _mapper.Map<List<InvitedUserResponse>>(networkManagerInvitations);

        return Ok(managedNetworkMangers);
    }

    [HttpGet("registered-users")]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<List<UserResponse>>> GetRegisteredUsersAsync(string userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", userId.ToSafeLog());
            return NotFound(); // Return 404 Not Found
        }

        _logger.LogInformation("Attempting to retrieve managed contributors for user ID: {UserId}", userId.ToSafeLog());

        var invitations = await _invitationService.GetByInviterUserIdAsync(user.Id);
        var invitedEmails = invitations.Select(i => i.InvitedEmail).Distinct().ToList();

        var registeredUsers = await _userService.GetRegisteredUsers(invitedEmails);

        // Always check for null before calling .Any()
        if (registeredUsers == null || !registeredUsers.Any())
        {
            _logger.LogInformation("No contributors found for user ID: {UserId}", userId.ToSafeLog());
            return Ok(new List<UserResponse>()); // Return an empty list to avoid null reference issues
        }

        // Exclude the responsible user from the contributors list
        var filteredUsers = registeredUsers.Where(ru => ru.EmailId != user.EmailId).ToList();

        _logger.LogInformation("Successfully retrieved {Count} managed contributors for user ID: {UserId}", filteredUsers.Count, userId.ToSafeLog());

        return Ok(_mapper.Map<List<UserResponse>>(filteredUsers));
    }


    [HttpGet("heat-network/{hnId}/roles")]
    [ProducesResponseType(typeof(List<UserRoleDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<ActionResult<List<UserRoleDetailResponse>>> GetHeatNetworkUsersWithRoles(string hnId)
    {
        if (string.IsNullOrWhiteSpace(hnId))
        {
            return BadRequest("Heat Network ID must be provided.");
        }

        // Get Responsible Party
        var rpUser = await _userService.GetResponsiblePartyByHnIdAsync(hnId);
        if (rpUser == null)
        {
            return NotFound($"No Responsible Party found for Heat Network ID: {hnId.ToSafeLog()}");
        }

        // Get other users with roles
        var result = await _userService.GetHeatNetworkUsersWithRolesAsync(hnId)
                     ?? new List<UserRoleDetailResponse>();

        // Insert RP user at the top
        result.Insert(0, _mapper.Map<UserRoleDetailResponse>(rpUser));

        return Ok(result);
    }


    /// <summary>
    /// Updates the OrgId associated with a specific user.
    /// </summary>
    /// <param name="request">The UserId and the NewOrgId.</param>
    [HttpPatch("update-orgid")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrgId([FromBody] UpdateUserOrgIdRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.UpdateOrgIdAsync(request.UserId, request.OrgId);

        if (result.IsAcknowledged == false)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, "Database update operation was not acknowledged.");
        }

        if (result.MatchedCount == 0)
        {
            return NotFound($"User with ID '{request.UserId}' not found.");
        }

        // 204 No Content is standard for a successful update where no data is returned
        return NoContent();
    }

    /// <summary>
    /// Gets all users belonging to a specific organisation ID.
    /// </summary>
    /// <param name="organisationId">The unique identifier of the organisation.</param>
    /// <returns>A list of User objects.</returns>
    [HttpGet("organisation/{organisationId}")] // Defines the HTTP GET route and parameter
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<User>>> GetUsersByOrganisation(string organisationId)
    {
        if (string.IsNullOrEmpty(organisationId))
        {
            return BadRequest("Organisation ID cannot be empty.");
        }

        // Call the service method to fetch data from MongoDB
        var users = await _userService.GetUsersByOrgIdAsync(organisationId);

        if (users == null || !users.Any())
        {
            // Optional: return 404 if no users are found for that ID
            return NotFound($"No users found for organisation ID: {organisationId}");
        }

        var usersResponse = _mapper.Map<List<UserResponse>>(users);

        return Ok(usersResponse);
    }

    private static List<HeatNetworkInfo> MapHeatNetworks(UserDetailsResult user)
    {
        var heatNetworks = UserNetworkHelper.GetAuthorizedNetworks(user);
        return heatNetworks?.Select(x => new HeatNetworkInfo
        {
            HnId = x.HnId,
            Name = x.Name
        }).ToList() ?? new List<HeatNetworkInfo>();
    }
}