Ensure a the key vault from *secrets* exists

# Create a secret
```
az keyvault key create --name testkey --vault-name fl-test --kty rsa 
```
Now, you can create different types and set specific permissions. You can also set expiry and not-before datetimes; unlike for Secrets, Key Vault will actually use these when deciding which key to use.

# Give my user access to cryptographic operations
```
az keyvault set-policy `
    --name fl-test `
    --upn "flytzen@neworbit.co.uk" `
    --key-permissions create, decrypt, encrypt, get, list, sign, unwrapKey, update, verify, wrapKey
```


# Demo encryption
See the one I created before

Note that one of the smart things here is that you can change the asymmetric key you use frequently; Key Vault will keep all the versions. As long as you keep the key identifier, you are golden.

# Demo signing
Show signing normally.
Then change the text and see it fail

Explain why this is useful - savings account example