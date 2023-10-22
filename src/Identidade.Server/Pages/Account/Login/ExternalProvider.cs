// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Identidade.Server.Pages.Account.Login
{
    public partial class ViewModel
    {
        public class ExternalProvider
        {
            public string DisplayName { get; set; }
            public string AuthenticationScheme { get; set; }
        }
    }
}