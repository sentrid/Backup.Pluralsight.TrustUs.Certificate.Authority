﻿using System.Collections.Generic;
using System.IO;
using Pluralsight.TrustUs.DataStructures;
using Pluralsight.TrustUs.Libraries;

namespace Pluralsight.TrustUs
{
    public class CertificateAuthoritySetup
    {
        /// <summary>
        ///     Installs the specified root certificate authority.
        /// </summary>
        /// <param name="rootCertificateAuthority">The root certificate authority.</param>
        /// <param name="intermediateCertificateAuthorities">The intermediate certificate authorities.</param>
        public void Install(CertificateAuthorityConfiguration rootCertificateAuthority,
            List<CertificateConfiguration> intermediateCertificateAuthorities)
        {
            GenerateRootCaCertificate(rootCertificateAuthority);
            InitializeCertificateStore(rootCertificateAuthority);
            foreach (var configuration in intermediateCertificateAuthorities)
                GenerateIntermediateCertificate(configuration);
        }

        /// <summary>
        ///     Generates the root ca certificate.
        /// </summary>
        /// <param name="rootCertificateAuthority">The root certificate authority.</param>
        private void GenerateRootCaCertificate(CertificateAuthorityConfiguration rootCertificateAuthority)
        {
            /* Create an RSA public/private key context, set a label for it, and generate a key into it */
            var caKeyPair = crypt.CreateContext(crypt.UNUSED, crypt.ALGO_RSA);

            crypt.SetAttributeString(caKeyPair, crypt.CTXINFO_LABEL, rootCertificateAuthority.KeyLabel);
            crypt.SetAttribute(caKeyPair, crypt.CTXINFO_KEYSIZE, 2048 / 8);
            crypt.GenerateKey(caKeyPair);

            var caKeyStore = crypt.KeysetOpen(crypt.UNUSED, crypt.KEYSET_FILE,
                rootCertificateAuthority.KeystoreFileName,
                crypt.KEYOPT_CREATE);
            crypt.AddPrivateKey(caKeyStore, caKeyPair, rootCertificateAuthority.PrivateKeyPassword);

            crypt.KeysetClose(caKeyStore);

            var certificate = crypt.CreateCert(crypt.UNUSED, crypt.CERTTYPE_CERTIFICATE);

            crypt.SetAttribute(certificate, crypt.CERTINFO_SUBJECTPUBLICKEYINFO, caKeyPair);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_COUNTRYNAME,
                rootCertificateAuthority.DistinguishedName.Country);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_STATEORPROVINCENAME,
                rootCertificateAuthority.DistinguishedName.State);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_LOCALITYNAME,
                rootCertificateAuthority.DistinguishedName.Locality);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_ORGANIZATIONNAME,
                rootCertificateAuthority.DistinguishedName.Organization);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_ORGANIZATIONALUNITNAME,
                rootCertificateAuthority.DistinguishedName.OrganizationalUnit);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_COMMONNAME,
                rootCertificateAuthority.DistinguishedName.CommonName);

            crypt.SetAttribute(certificate, crypt.CERTINFO_SELFSIGNED, 1);
            crypt.SetAttribute(certificate, crypt.CERTINFO_CA, 1);

            crypt.SetAttribute(certificate, crypt.ATTRIBUTE_CURRENT, crypt.CERTINFO_AUTHORITYINFO_CERTSTORE);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_UNIFORMRESOURCEIDENTIFIER,
                rootCertificateAuthority.CertStoreUrl);

            crypt.SetAttribute(certificate, crypt.ATTRIBUTE_CURRENT, crypt.CERTINFO_AUTHORITYINFO_OCSP);
            crypt.SetAttributeString(certificate, crypt.CERTINFO_UNIFORMRESOURCEIDENTIFIER,
                rootCertificateAuthority.OcspUrl);

            crypt.SignCert(certificate, caKeyPair);

            crypt.AddPublicKey(caKeyStore, certificate);

            var dataSize = crypt.ExportCert(null, 0, crypt.CERTFORMAT_CERTIFICATE, certificate);
            var exportedCert = new byte[dataSize];
            crypt.ExportCert(exportedCert, dataSize, crypt.CERTFORMAT_CERTIFICATE, certificate);

            File.WriteAllBytes(rootCertificateAuthority.CertificateFileName, exportedCert);

            
            crypt.DestroyContext(caKeyPair);
            crypt.DestroyCert(certificate);
        }

        /// <summary>
        ///     Initializes the certificate store.
        /// </summary>
        private void InitializeCertificateStore(CertificateAuthorityConfiguration rootCertificateAuthority)
        {
            if (!File.Exists(rootCertificateAuthority.CertificateStoreFilePath))
            {
                var file = File.Create(rootCertificateAuthority.CertificateStoreFilePath);
                file.Close();
            }

            var certStore = crypt.KeysetOpen(crypt.UNUSED, crypt.KEYSET_ODBC_STORE, rootCertificateAuthority.CertificateStoreOdbcName, crypt.KEYOPT_CREATE);
            crypt.KeysetClose(certStore);
        }

        /// <summary>
        ///     Generates the intermediate certificate.
        /// </summary>
        /// <param name="certificateConfiguration">The certificate configuration.</param>
        private void GenerateIntermediateCertificate(CertificateConfiguration certificateConfiguration)
        {
            /***************************************************************/
            /*                  Get the CA Certificate                     */
            /***************************************************************/
            var caKeyStore = crypt.KeysetOpen(crypt.UNUSED, crypt.KEYSET_FILE,
                certificateConfiguration.SigningKeyFileName,
                crypt.KEYOPT_READONLY);
            var caPrivateKey = crypt.GetPrivateKey(caKeyStore, crypt.KEYID_NAME,
                certificateConfiguration.SigningKeyLabel,
                certificateConfiguration.SigningKeyPassword);

            /* Create an RSA public/private key context, set a label for it, and generate a key into it */
            var icaKeyPair = crypt.CreateContext(crypt.UNUSED, crypt.ALGO_RSA);

            crypt.SetAttributeString(icaKeyPair, crypt.CTXINFO_LABEL, certificateConfiguration.KeyLabel);
            crypt.SetAttribute(icaKeyPair, crypt.CTXINFO_KEYSIZE, 2048 / 8);
            crypt.GenerateKey(icaKeyPair);

            var icaKeyStore = crypt.KeysetOpen(crypt.UNUSED, crypt.KEYSET_FILE,
                certificateConfiguration.KeystoreFileName,
                crypt.KEYOPT_CREATE);
            crypt.AddPrivateKey(icaKeyStore, icaKeyPair, certificateConfiguration.PrivateKeyPassword);

            crypt.KeysetClose(icaKeyStore);

            var certChain = crypt.CreateCert(crypt.UNUSED, crypt.CERTTYPE_CERTCHAIN);

            crypt.SetAttribute(certChain, crypt.CERTINFO_SUBJECTPUBLICKEYINFO, icaKeyPair);
            crypt.SetAttributeString(certChain, crypt.CERTINFO_COUNTRYNAME,
                certificateConfiguration.DistinguishedName.Country);
            crypt.SetAttributeString(certChain, crypt.CERTINFO_ORGANIZATIONNAME,
                certificateConfiguration.DistinguishedName.Organization);
            crypt.SetAttributeString(certChain, crypt.CERTINFO_ORGANIZATIONALUNITNAME,
                certificateConfiguration.DistinguishedName.OrganizationalUnit);
            crypt.SetAttributeString(certChain, crypt.CERTINFO_COMMONNAME,
                certificateConfiguration.DistinguishedName.CommonName);
            crypt.SetAttribute(certChain, crypt.CERTINFO_CA, 1);

            crypt.SignCert(certChain, caPrivateKey);
            crypt.AddPublicKey(icaKeyStore, certChain);

            var dataSize = crypt.ExportCert(null, 0, crypt.CERTFORMAT_CERTIFICATE, certChain);
            var exportedCert = new byte[dataSize];
            crypt.ExportCert(exportedCert, dataSize * 2, crypt.CERTFORMAT_CERTIFICATE, certChain);

            File.WriteAllBytes(certificateConfiguration.CertificateFileName, exportedCert);

            crypt.DestroyCert(certChain);
            
            crypt.KeysetClose(caKeyStore);
            crypt.DestroyContext(caPrivateKey);
            crypt.DestroyContext(icaKeyPair);
        }
    }
}