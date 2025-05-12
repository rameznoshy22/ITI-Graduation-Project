﻿using Data_access_layer.model;
using Educational_Platform.ViewModel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Business_logic_layer.interfaces;

namespace Educational_Platform.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailSender _emailSender;
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IEmailSender emailSender
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _emailSender = emailSender;
        
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (!user.EmailConfirmed)
                    {
                        ViewBag.ShowResend = true;
                        ViewBag.UserEmail = user.Email;
                        ModelState.AddModelError(string.Empty, "بريدك الإلكتروني غير مفعل. اضغط لإعادة إرسال رابط التفعيل.");
                        return View(model);
                    }

                    var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, model.RememberMe);
                    if (result.Succeeded)
                    {
                        // Build custom claims
                        var userClaims = new List<Claim>
                {
                    new Claim("FullName", user.FullName),
                    new Claim("IsActive", user.IsActive ? "1" : "0"),
                    new Claim("ProfilePicture", user.ProfilePicture ?? "")
                };

                        // Add custom claims to the user
                        var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                        await _signInManager.SignInWithClaimsAsync(user, model.RememberMe, identity.Claims);

                        // Redirect based on role
                        if (await _userManager.IsInRoleAsync(user, "Student"))
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        else if (await _userManager.IsInRoleAsync(user, "Instructor"))
                        {
                            return RedirectToAction("Index", "Lesson");
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "المستخدم غير موجود.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "المستخدم غير موجود.");
                }
            }
            return View(model);
        }



        // Registration for Students
        [HttpGet]

        public IActionResult RegisterStudent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStudent(RegisterStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Start a transaction as we'll be making multiple database operations
            using var transaction = await _context.Database.BeginTransactionAsync();
            var user = new ApplicationUser { };

            try
            {
                // Process profile picture if uploaded
                string profilePicturePath = model.ProfilePicture;
                if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
                {
                    try
                    {
                        profilePicturePath = Helper.Helper.uploadfile(model.ProfilePictureFile, "studentImage");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ProfilePictureFile", "فشل تحميل صورة الملف");
                        return View(model);
                    }
                }

                // Create user
                 user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    ProfilePicture = profilePicturePath,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Ensure Student role exists
                if (!await _roleManager.RoleExistsAsync("Student"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Student"));
                }

                // Assign Student role
                var roleResult = await _userManager.AddToRoleAsync(user, "Student");
                if (!roleResult.Succeeded)
                {
                    // Continue anyway as this might be handled later by an admin
                }

                // Create Student entity
                var student = new Student
                {
                    Email = model.Email,
                    Name = $"{model.FirstName} {model.LastName}",
                    PhoneNumber = model.PhoneNumber,
                    FatherPhone = model.FatherPhoneNumber,
                    GradeLevel = model.GradeLevel,
                    ProfilePicture = profilePicturePath,
                    UserId = user.Id
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Link ApplicationUser to Student
                user.StudentId = student.ID;
                await _userManager.UpdateAsync(user);

                // Commit transaction if everything succeeded
                await transaction.CommitAsync();

                // Send confirmation email (example - implement SendConfirmationEmailAsync)
                 try
                 {
                     var confirmationLink = await ManageToken(user.Id);

                     string emailBody = $@"
 <div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, Arial, sans-serif; color: #333;"">
     <h2 style=""color: #0066cc;"">مرحبًا بك في منصتنا التعليمية!</h2>
     <p>شكرًا للتسجيل. يرجى تأكيد عنوان بريدك الإلكتروني بالنقر على الرابط أدناه:</p>
     <p style=""margin: 25px 0;"">
         <a href='{HtmlEncoder.Default.Encode(confirmationLink)}' 
            style=""background-color: #0066cc; color: white; padding: 10px 20px; 
                   text-decoration: none; border-radius: 5px; font-weight: bold;"">
             تأكيد بريدك الإلكتروني
         </a>
     </p>
     <p style=""color: #666; font-size: 14px;"">إذا لم تقم بالتسجيل للحصول على حساب، يرجى تجاهل هذا البريد الإلكتروني.</p>
     <p style=""color: #666; font-size: 12px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 10px;"">
         هذا البريد الإلكتروني صالح لمدة 5 دقائق فقط.
     </p>
 </div>
 ";

                     await _emailSender.SendEmailAsync(model.Email, "تأكيد حساب بريدك الإلكتروني", emailBody);

                     TempData["SuccessMessage"] = "تم إنشاء حسابك بنجاح! يرجى التحقق من بريدك الإلكتروني لتأكيد حسابك.";
                     return RedirectToAction("Login", "Account");
                 }
                 catch (Exception ex)
                 {
                     var errorMessage = $"تم إنشاء الحساب، ولكن فشل إرسال بريد التأكيد: {ex.Message}";
                     if (ex.InnerException != null)
                     {
                         errorMessage += $" استثناء داخلي: {ex.InnerException.Message}";
                     }
                     ModelState.AddModelError(string.Empty, errorMessage);
                     return View(model);
                 }
                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "حدث خطأ أثناء التسجيل. يرجى المحاولة مرة أخرى.");
                return View(model);

            }
        }
        // Registration for Instructors
        [HttpGet]
        public IActionResult RegisterInstructor()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterInstructor(RegisterInstructorViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign Instructor role
                    if (!await _roleManager.RoleExistsAsync("Instructor"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Instructor"));
                    }
                    await _userManager.AddToRoleAsync(user, "Instructor");

                    // Create Instructor entity
                    var instructor = new Instructor
                    {
                        Email = model.Email,
                        Name = $"{model.FirstName} {model.LastName}",
                        UserId = user.Id
                    };

                    _context.Instructors.Add(instructor);
                    await _context.SaveChangesAsync();

                    // Link ApplicationUser to Instructor
                    user.InstructorId = instructor.ID;
                    await _userManager.UpdateAsync(user);

                    // Redirect to profile completion
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        //// Edit Student Profile
        //[HttpGet]
        //[Authorize(Roles = "Student")]
        //public async Task<IActionResult> EditStudentProfile()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null) return NotFound();

        //    var student = _context.Students.FirstOrDefault(s => s.ID == user.StudentId);
        //    if (student == null) return NotFound();

        //    var model = new StudentProfileViewModel
        //    {
        //        Id = user.Id,
        //        Email = user.Email,
        //        FirstName = user.FirstName,
        //        LastName = user.LastName,
        //        PhoneNumber = user.PhoneNumber,
        //        FatherPhone = student.FatherPhone,
        //        GradeLevel = student.GradeLevel,
        //        CurrentProfilePicture = user.ProfilePicture
        //    };

        //    return View(model);
        //}

        //[HttpPost]
        //[Authorize(Roles = "Student")]
        //public async Task<IActionResult> EditStudentProfile(StudentProfileViewModel model)
        //{
        //    if (!ModelState.IsValid) return View(model);

        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null) return NotFound();

        //    var student = _context.Students.FirstOrDefault(s => s.ID == user.StudentId);
        //    if (student == null) return NotFound();

        //    // Update profile picture if uploaded
        //    if (model.ProfilePictureFile != null)
        //    {
        //        string uniqueFileName = await ProcessProfilePictureAsync(model.ProfilePictureFile);
        //        user.ProfilePicture = uniqueFileName;
        //        student.ProfilePicture = uniqueFileName;
        //    }

        //    // Update user and student details
        //    user.FirstName = model.FirstName;
        //    user.LastName = model.LastName;
        //    user.PhoneNumber = model.PhoneNumber;
        //    user.UpdatedAt = DateTime.UtcNow;

        //    student.Name = $"{model.FirstName} {model.LastName}";
        //    student.PhoneNumber = model.PhoneNumber;
        //    student.FatherPhone = model.FatherPhone;
        //    student.GradeLevel = model.GradeLevel;

        //    await _userManager.UpdateAsync(user);
        //    _context.Update(student);
        //    await _context.SaveChangesAsync();

        //    TempData["StatusMessage"] = "Profile updated successfully.";
        //    return RedirectToAction("Index", "Student");
        //}

        // Edit Instructor Profile
        [HttpGet]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> EditInstructorProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var instructor = _context.Instructors.FirstOrDefault(i => i.ID == user.InstructorId);
            if (instructor == null) return NotFound();

            var model = new InstructorProfileViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Subjects = instructor.Subjects,
                Qualifications = instructor.Qualifications,
                Bio = instructor.Bio,
                CurrentProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> EditInstructorProfile(InstructorProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var instructor = _context.Instructors.FirstOrDefault(i => i.ID == user.InstructorId);
            if (instructor == null) return NotFound();

            // Update profile picture if uploaded
            if (model.ProfilePictureFile != null)
            {
                string uniqueFileName = await ProcessProfilePictureAsync(model.ProfilePictureFile);
                user.ProfilePicture = uniqueFileName;
                instructor.Photo = uniqueFileName;
            }

            // Update user and instructor details
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            instructor.Name = $"{model.FirstName} {model.LastName}";
            instructor.PhoneNumber = model.PhoneNumber;
            instructor.Subjects = model.Subjects;
            instructor.Qualifications = model.Qualifications;
            instructor.Bio = model.Bio;

            await _userManager.UpdateAsync(user);
            _context.Update(instructor);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Profile updated successfully.";
            return RedirectToAction("Index", "Instructor");
        }

        // Helper method to process profile pictures
        private async Task<string> ProcessProfilePictureAsync(IFormFile profilePicture)
        {
            if (profilePicture == null) return null;

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + profilePicture.FileName;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/profiles");
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            Directory.CreateDirectory(uploadsFolder);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profilePicture.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }
        
        public async Task<dynamic> ManageToken(string id)
        {
            // Generate a random token
            var rawToken = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddMinutes(5);

            var confirmationToken = new EmailConfirmationToken
            {
                Token = rawToken,
                UserId = id,
                ExpiryDate = expiry,
                IsUsed = false
            };
            _context.EmailConfirmationTokens.Add(confirmationToken);
            await _context.SaveChangesAsync();

            // Build the confirmation link (token only)
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { token = rawToken }, Request.Scheme);
            return confirmationLink;

        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendActivationEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && !user.EmailConfirmed)
            {

                var confirmationLink = await ManageToken(user.Id);

                string emailBody = $@"
<div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, Arial, sans-serif; color: #333;"">
    <h2 style=""color: #0066cc;"">مرحبًا بك في منصتنا التعليمية!</h2>
    <p>شكرًا للتسجيل. يرجى تأكيد عنوان بريدك الإلكتروني بالنقر على الرابط أدناه:</p>
    <p style=""margin: 25px 0;"">
        <a href='{HtmlEncoder.Default.Encode(confirmationLink)}' 
           style=""background-color: #0066cc; color: white; padding: 10px 20px; 
                  text-decoration: none; border-radius: 5px; font-weight: bold;"">
            تأكيد بريدك الإلكتروني
        </a>
    </p>
    <p style=""color: #666; font-size: 14px;"">إذا لم تقم بالتسجيل للحصول على حساب، يرجى تجاهل هذا البريد الإلكتروني.</p>
    <p style=""color: #666; font-size: 12px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 10px;"">
        هذا البريد الإلكتروني صالح لمدة 24 ساعة فقط.
    </p>
</div>
";

                await _emailSender.SendEmailAsync(user.Email, "تأكيد بريدك الإلكتروني", emailBody);
            }
            TempData["SuccessMessage"] = "تم إرسال رابط التفعيل مرة أخرى إلى بريدك الإلكتروني.";
            return RedirectToAction("Login");
        }
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Index", "Home");

            var confirmationToken = await _context.EmailConfirmationTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (confirmationToken == null || confirmationToken.IsUsed || confirmationToken.ExpiryDate < DateTime.UtcNow)
            {
                // Invalid or expired or already used
                return View("InvalidOrExpiredToken");
            }

            var user = await _userManager.FindByIdAsync(confirmationToken.UserId);
            if (user == null)
                return NotFound($"Unable to load user.");

            // Confirm the email
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);

            // Mark token as used
            confirmationToken.IsUsed = true;
            await _context.SaveChangesAsync();

            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {

                return RedirectToAction("ForgotPasswordConfirmation");
            }


            var rawToken = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddMinutes(5);


            var resetToken = new PasswordResetToken
            {
                Token = rawToken,
                UserId = user.Id,
                ExpiryDate = expiry,
                IsUsed = false
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();


            var callbackUrl = Url.Action("ResetPassword", "Account",
                new { token = rawToken }, Request.Scheme);


            string emailBody = $@"
<div dir=""rtl"" style=""font-family: 'Segoe UI', Tahoma, Arial, sans-serif; color: #333;"">
    <h2 style=""color: #0066cc;"">طلب إعادة تعيين كلمة المرور</h2>
    <p>لقد طلبت مؤخرًا إعادة تعيين كلمة المرور لحسابك.</p>
    <p>يرجى النقر على الرابط أدناه لإعادة تعيين كلمة المرور:</p>
    <p style=""margin: 25px 0;"">
        <a href='{HtmlEncoder.Default.Encode(callbackUrl)}' 
           style=""background-color: #0066cc; color: white; padding: 10px 20px; 
                  text-decoration: none; border-radius: 5px; font-weight: bold;"">
            إعادة تعيين كلمة المرور
        </a>
    </p>
    <p style=""color: #666; font-size: 14px;"">إذا لم تطلب إعادة تعيين كلمة المرور، يرجى تجاهل هذا البريد الإلكتروني أو التواصل مع الدعم.</p>
    <p style=""color: #666; font-size: 12px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 10px;"">
        ستنتهي صلاحية هذا الرابط خلال 5 دقائق.
    </p>
</div>
";

            await _emailSender.SendEmailAsync(model.Email, "إعادة تعيين كلمة المرور", emailBody);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Index", "Home");

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiryDate < DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "رابط إعادة تعيين كلمة المرور منتهي الصلاحية أو غير صالح.";
                return View("InvalidOrExpiredToken");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                UserId = resetToken.UserId
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // البحث والتحقق من الرمز
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == model.Token);

            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiryDate < DateTime.UtcNow)
            {
                ModelState.AddModelError(string.Empty, "رمز إعادة تعيين كلمة المرور منتهي الصلاحية أو غير صالح.");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(resetToken.UserId);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, identityToken, model.Password);

            if (result.Succeeded)
            {
                resetToken.IsUsed = true;
                await _context.SaveChangesAsync();

                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        public async Task<IActionResult> SignOut()
        {
            // Sign out from Identity
            await _signInManager.SignOutAsync();

            // Clear authentication cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Clear session if available
            try
            {
                if (HttpContext.Session != null)
                {
                    HttpContext.Session.Clear();
                }
            }
            catch (InvalidOperationException)
            {
                // Sessions not configured, just continue
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
