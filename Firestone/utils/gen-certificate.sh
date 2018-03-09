#!/bin/bash

# Generate a self-signed SSL certificate for use with Firestone
# Written by Katy Coe 2018
# http://www.hearthcode.org

# Usage: ./gen-certificate.sh <fully_qualified_domain_name_of_your_server>
# To use locally, specify localhost as the domain
# NOTE: Ensure you run the above command from the utils folder which can be found in the root folder of the compiled application

DOMAIN=$1

if [ -z "$DOMAIN" ]
then
	echo "Usage: gen-certificate.sh <fully_qualified_domain_name_of_your_server>"
	exit
fi

openssl genrsa -aes128 -passout pass:firestone -out firestone-private.pem 2048 >/dev/null 2>/dev/null
openssl req -x509 -new -passin pass:firestone -key firestone-private.pem -out firestone-public.pem -subj "/C=US/O=hearthcode.org/CN=$DOMAIN"
openssl pkcs12 -export -in firestone-public.pem -inkey firestone-private.pem -out ../Firestone.pfx -passin pass:firestone -passout pass:firestone
rm -f firestone-private.pem firestone-public.pem
