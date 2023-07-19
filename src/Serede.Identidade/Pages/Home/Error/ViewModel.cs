// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using IdentityServer4.Models;

namespace Serede.Identidade.Pages.Error
{
    public class ViewModel
    {
        public ViewModel()
        {
        }

        public ViewModel(string error)
        {
            Error = new ErrorMessage { Error = error };
        }

        public ErrorMessage Error { get; set; }
    }
}