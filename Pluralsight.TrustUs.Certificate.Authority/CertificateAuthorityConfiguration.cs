﻿namespace Pluralsight.TrustUs
{
    public class CertificateAuthorityConfiguration : CertificateConfiguration
    {
        public string CertStoreUrl { get; set; }

        public string RevocationListUrl { get; set; }

        public string OcspUrl { get; set; }

    }
}