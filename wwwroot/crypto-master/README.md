# crypto
crypto with ed25519 + base58 or other

## demo
1. [ed25519 with base58][demo_ed25519_server]

## dependency 
1. `nacl` or `nacl-fast` can ref ***[tweetnacl-js][tweetnacl-js]***

2. `basex` and `base58` can ref ***[meteor-base58][meteor-base58]***

demo import
```
<script type="text/javascript" src="../js/encrypt/base/basex.js" ></script>
<script type="text/javascript" src="../js/encrypt/base/base58.js" ></script>
<script type="text/javascript" src="../js/encrypt/ed25519/nacl-fast.js" ></script>
<script type="text/javascript" src="../js/utils/encrypt-ed25519.js" ></script>
```

## usage
1. Generate KeyPair
`EncryptUtils.generateKeyPair = function(){...};`
```
var keyPair = EncryptUtils.generateKeyPair();
var keyPair_publicKey = keyPair.publicKey;
var keyPair_privateKey = keyPair.privateKey;
```

2. Sign with privateKey
`EncryptUtils.sign = function(privateKey, msg){...}`
```
EncryptUtils.sign(privateKey, msg);
```

3. Verify the msg with sig and publicKey
`EncryptUtils.verify = function(msg, sig, publicKey){...}`
```
EncryptUtils.verify(msg, sig, publicKey);
```

You also can view the file ***[ed25519_test.html][demo_ed25519]*** in demo dir.

[tweetnacl-js]:https://github.com/dchest/tweetnacl-js
[meteor-base58]:https://github.com/gghez/meteor-base58
[demo_ed25519]:/demo/ed25519_test.html
[demo_ed25519_server]:https://www.futurever.com/crypto/
