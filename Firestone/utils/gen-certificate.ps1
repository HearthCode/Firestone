# Generate a self-signed SSL certificate for use with Firestone
# Written by Katy Coe 2018
# https://github.com/HearthCode/Firestone
# http://www.hearthcode.org

param([string]$domain)

# Usage (you must have administrator privileges): powershell -file gen-certificate.ps1 -domain <fully_qualified_domain_name_of_your_server>
# To use locally, specify localhost as the domain
# NOTE: Ensure you run the above command from the utils folder which can be found in the root folder of the compiled application

$thumbprint = New-SelfSignedCertificate -CertStoreLocation Cert:\LocalMachine\My -DnsName $domain
$pwd = ConvertTo-SecureString -String "firestone" -Force -AsPlainText
Export-PfxCertificate -Cert "Cert:\LocalMachine\My\$($thumbprint.Thumbprint)" -FilePath ..\Firestone.pfx -Password $pwd
Remove-Item -Path "Cert:\LocalMachine\My\$($thumbprint.Thumbprint)" -DeleteKey
