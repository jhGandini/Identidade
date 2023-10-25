using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Identidade.Server.Models;
using Identidade.Server.Services;

namespace Identidade.Server.Pages.Account.ForgotPassword
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<SeredeUser> _userManager;
        private readonly EmailService _emailSender;

        public ForgotPasswordModel(UserManager<SeredeUser> userManager, EmailService emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
                {
                    //Don't reveal that the user does not exist or is not confirmed
                    return RedirectToPage("/Account/ForgotPassword/ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please 
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var email = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(Input.Email));
                var callbackUrl = Url.Page(
                    "/Account/ForgotPassword/ResetPassword",
                    pageHandler: null,
                    values: new { email, code },
                    protocol: Request.Scheme);

                var listaEmails = new List<string>()
                {
                    Input.Email
                };

                await _emailSender.EnviarEmail(
                    listaEmails,
                    "Reset Password",
                    $"Redefina sua senha por <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicando aqui</a>.");

                return RedirectToPage("/Account/ForgotPassword/ForgotPasswordConfirmation");
            }

            return Page();
        }
    }
}
