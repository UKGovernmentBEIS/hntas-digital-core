using AutoMapper;
using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Data.Models;
using HNTAS.Core.Api.Enums;
using HNTAS.Core.Api.Helpers;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.Models.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Mime;

namespace HNTAS.Core.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;
        private readonly IGovUkNotifyService _emailService; 
        private readonly ICounterService _orgCounterService; 
        private readonly NotificationSettings _notificationSettings;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, 
            ILogger<UsersController> logger, 
            IGovUkNotifyService emailService, 
            ICounterService orgCounterService, 
            IOptions<NotificationSettings> options,
            IMapper mapper)
        {
            _userService = userService;
            _logger = logger;
            _emailService = emailService;
            _orgCounterService = orgCounterService;
            _notificationSettings = options.Value;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves a list of all users.
        /// </summary>
        /// <returns>A list of user objects.</returns>
        [HttpGet] // This defines the route as GET /api/users
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserResponse>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<UserResponse>>> GetUsers()
        {
            _logger.LogInformation("Attempting to retrieve all users.");
            try
            {
                var users = await _userService.GetAsync();

                var usersResponse = _mapper.Map<List<UserResponse>>(users);

                _logger.LogInformation("Successfully retrieved {UserCount} users.", users.Count);

                return Ok(usersResponse); // Returns 200 OK with the list of users
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
        ///   A <see cref="StatusCodes.Status200OK"/> (OK) response with the found user object,
        ///   or a <see cref="StatusCodes.Status404NotFound"/> (Not Found) response if no user matches the provided ID.
        /// </returns>
        [HttpGet("{id:length(24)}", Name = "GetUserById")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)] // Explicitly defines success response type
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Explicitly defines not found response
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Good practice to include for general errors
        public async Task<ActionResult<UserResponse>> GetById(string id)
        {
            // Add logging for incoming request if desired
            _logger.LogInformation("Attempting to retrieve user with ID: {Id}", id);

            var user = await _userService.GetByIdAsync(id);

            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found.", id);
                return NotFound();
            }

            var userResponse = _mapper.Map<UserResponse>(user);

            _logger.LogInformation("Successfully retrieved user with ID: {Id}", id);

            return Ok(userResponse);
        }


        /// <summary>
        /// Get a User by their OneLogin ID
        /// </summary>
        /// <param name="oneLoginId"></param>
        /// <returns></returns>
        [HttpGet("onelogin/{oneLoginId}", Name = "GetUserByOneLoginId")]
        [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)] // Explicitly defines success response type
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Explicitly defines not found response
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Good practice to include for general errors
        public async Task<ActionResult<UserResponse>> GetUserByOneLoginId(string oneLoginId)
        {
            // Add logging for incoming request if desired
            _logger.LogInformation("Attempting to retrieve user with ID: {Id}", oneLoginId);

            var user = await _userService.GetByUserOneLoginIdAsync(oneLoginId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {Id} not found.", oneLoginId);
                return NotFound();
            }

            _logger.LogInformation("Successfully retrieved user with ID: {Id}", oneLoginId);

            var userResponse = _mapper.Map<UserResponse>(user);

            return Ok(userResponse);
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
                _logger.LogWarning("Invalid initial registration data for UserId: {UserId}, EmailId: {EmailId}. Errors: {Errors}",
                    registrationData.OneLoginId, registrationData.EmailId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
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
                        Detail = $"A user with the provided UserId ({registrationData.OneLoginId}) already exists."
                    });
                }

                var newUser = new User
                {
                    OneLoginId = registrationData.OneLoginId,
                    EmailId = registrationData.EmailId,
                    Status = registrationData.Status
                };

                await _userService.CreateAsync(newUser);

                _logger.LogInformation("New user initially registered: {UserId} (DB Id: {Id})", newUser.OneLoginId, newUser.Id);

                return StatusCode(StatusCodes.Status201Created, newUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during initial user registration for UserId: {UserId}, EmailId: {EmailId}", registrationData.OneLoginId, registrationData.EmailId);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred during initial user registration."
                });
            }
        }

        /// <summary>
        /// Update Organization Details for a User
        /// </summary>
        /// <remarks>Updates specific organization details for an existing user and generates an OrgId if not already set.
        /// Changes user status to 'Active' upon successful update. Returns the fully updated user object.</remarks>
        /// <param name="id">The  ID of the user to update.</param>
        /// <param name="request">The organization details to update.</param>
        /// <returns>The fully updated user object if successful.</returns>
        [HttpPatch("{id:length(24)}/org-details")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(User), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<User>> UpdateOrgDetails(string id, [FromBody] UpdateOrgDetailsAndRolesRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid organisation details update data for user ID: {UserId}. Errors: {Errors}",
                    id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return ValidationProblem(ModelState);
            }

            var existingUser = await _userService.GetByIdAsync(id);

            if (existingUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for organisation details update.", id);
                return NotFound();
            }

            try
            {
                if (existingUser.OrgDetails == null)
                {
                    existingUser.OrgDetails = new OrgDetails();
                }

                // Map properties from orgDetailsAndRolesRequest payload#
                existingUser.OrgDetails.OrgType = request.OrgDetails.OrgType;
                existingUser.OrgDetails.CompaniesHouseNumber = request.OrgDetails.CompaniesHouseNumber;
                existingUser.OrgDetails.OrgName = request.OrgDetails.OrgName;
                existingUser.OrgDetails.OrgRegisteredAddress = request.OrgDetails.OrgRegisteredAddress;

                //Rp contact details
                existingUser.OrgDetails.PreferredContactType = request.OrgDetails.PreferredContactType;
                existingUser.OrgDetails.LandlineNumber = request.OrgDetails.LandlineNumber;
                existingUser.OrgDetails.ContactNumberExtension = request.OrgDetails.ContactNumberExtension;
                existingUser.OrgDetails.MobileNumber = request.OrgDetails.MobileNumber;
                existingUser.OrgDetails.JobTitle = request.OrgDetails.JobTitle;
                existingUser.OrgDetails.FirstName = request.OrgDetails.FirstName;
                existingUser.OrgDetails.LastName = request.OrgDetails.LastName;

                if(existingUser.Roles == null)
                {
                    existingUser.Roles = new List<UserRole>() { request.Role };
                }
                else if (!existingUser.Roles.Contains(request.Role))
                {
                    existingUser.Roles.Add(request.Role);
                }


                switch (request.OrgDetails.PreferredContactType) // Use orgDetailsAndRolesRequest for validation logic
                {
                    case PreferredContactType.Landline:
                        // If Landline is preferred, MobileNumber should be nullified and its validation removed
                        existingUser.OrgDetails.MobileNumber = null; // Update the object that will be saved
                        ModelState.Remove(nameof(request.OrgDetails.MobileNumber)); // Remove any existing errors for MobileNumber

                        if (string.IsNullOrWhiteSpace(request.OrgDetails.LandlineNumber))
                        {
                            ModelState.AddModelError(nameof(request.OrgDetails.LandlineNumber), "Enter your landline number.");
                        }
                        break;
                    case PreferredContactType.Mobile:
                        // If Mobile is preferred, LandlineNumber and Extension should be nullified and their validation removed
                        existingUser.OrgDetails.LandlineNumber = null;
                        existingUser.OrgDetails.ContactNumberExtension = null;
                        ModelState.Remove(nameof(request.OrgDetails.LandlineNumber));
                        ModelState.Remove(nameof(request.OrgDetails.ContactNumberExtension));

                        if (string.IsNullOrWhiteSpace(request.OrgDetails.MobileNumber))
                        {
                            ModelState.AddModelError(nameof(request.OrgDetails.MobileNumber), "Enter your mobile number.");
                        }
                        break;
                    default:
                        // If other or no preference, clear all numbers and remove their validation
                        existingUser.OrgDetails.LandlineNumber = null;
                        existingUser.OrgDetails.ContactNumberExtension = null;
                        existingUser.OrgDetails.MobileNumber = null;
                        ModelState.Remove(nameof(request.OrgDetails.LandlineNumber));
                        ModelState.Remove(nameof(request.OrgDetails.ContactNumberExtension));
                        ModelState.Remove(nameof(request.OrgDetails.MobileNumber));
                        break;
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Conditional validation failed for org details update for user ID: {UserId}. Errors: {Errors}",
                        id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return ValidationProblem(ModelState);
                }

                // Generate OrgId if it's not already set or is 0
                if (string.IsNullOrWhiteSpace(existingUser.OrgDetails.OrgId))
                {
                    long nextSequence = await _orgCounterService.GetNextSequenceValue("orgId_sequence");
                    existingUser.OrgDetails.OrgId = $"ORG{nextSequence:D7}";
                }


                await _userService.UpdateAsync(id, existingUser);

                _logger.LogInformation("Organisation details and status updated for user {UserId}. Generated OrgId: {OrgId}", id, existingUser.OrgDetails.OrgId);

                await TrySendOrgCreatedEmailAsync(existingUser);

                return Ok(existingUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organisation details for user {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred while updating organization details."
                });
            }
        }


        /// <summary>
        /// Deletes a user by their ID.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>
        /// A 204 No Content response if the user was successfully deleted.
        /// A 404 Not Found response if the user with the specified ID does not exist.
        /// A 500 Internal Server Error for unexpected issues.
        /// </returns>
        [HttpDelete("{id}")] // This defines the route as DELETE /api/users/{id}
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            _logger.LogInformation("Attempting to delete user with ID: {UserId}", id);

            // First, optionally check if the user exists before attempting to delete.
            // This is good practice to return a 404 rather than just a 200/204 on non-existent delete.
            var existingUser = await _userService.GetByIdAsync(id); // Assuming you have a GetByIdAsync
            if (existingUser == null)
            {
                _logger.LogWarning("Delete request for user ID: {UserId} failed. User not found.", id);
                return NotFound(); // Return 404 Not Found
            }

            try
            {
                // Changed to RemoveAsync and no longer checking a boolean return value
                await _userService.RemoveAsync(id);

                _logger.LogInformation("User with ID: {UserId} successfully removed.", id);
                return NoContent(); // Return 204 No Content
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user with ID: {UserId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while deleting the user.");
            }
        }

        // --- Private Helper Method ---
        private async Task TrySendOrgCreatedEmailAsync(User user)
        {
            if (user?.OrgDetails == null || string.IsNullOrWhiteSpace(user.EmailId) || string.IsNullOrWhiteSpace(user.OrgDetails.OrgId))
            {
                _logger.LogInformation("Skipping email: missing User, OrgDetails, EmailId, or OrgId for user {UserId}", user?.Id);
                return;
            }

            string orgName = user.OrgDetails.OrgName ?? "Your Organization";
            string firstName = StringFormatter.ToTitleCaseSingleWord(user.OrgDetails.FirstName ?? "");
            string lastName = StringFormatter.ToTitleCaseSingleWord(user.OrgDetails.LastName ?? "");
            string fullName = $"{firstName} {lastName}".Trim();

            string formattedAddress = StringFormatter.FormatAddress(user.OrgDetails.OrgRegisteredAddress);

            var emailSent = await _emailService.SendEmailAsync(
                user.EmailId,
                _notificationSettings.OrgCreatedEmailTemplateId,
                new Dictionary<string, dynamic>
                {
                    { "orgName", orgName },
                    { "orgId", user.OrgDetails.OrgId },
                    { "fullName", fullName },
                    { "address", formattedAddress }
                }
            );

            if (emailSent)
                _logger.LogInformation("Email sent successfully to {EmailId} for user {UserId}", user.EmailId, user.Id);
            else
                _logger.LogWarning("Email failed to send to {EmailId} for user {UserId}", user.EmailId, user.Id);
        }
    }
}

