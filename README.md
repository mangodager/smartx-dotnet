# smartx-dotnet
The redesign and re implementation of smartx is different from the consensus and storage of java version, which is implemented by C#

# English development logger

### 2020-10-29 Project announcement
Smartx-dotnet's bidding shard chain mining tutorial
https://smartx.one/archives/4387
After meeting the conditions of cloud server and long-term operation (1-2 months), you can study the tutorial first. After studying, apply for 100w test coin with the group owner

### 2020-10-31 Project announcement
There are some bugs in the node program, which caused beruler to not be added to the POS block list on 10-31. It has been repaired so far, please follow the tutorial and try again.

### 2020-11-01 Project announcement
If you want to be a bidding node, download node 1.3 version first. Then first update the bin patch to solve the problem of pulling blocks and being unable to be added to the rukes list on 1031, covering it directly, without requiring everyone to update. Everyone will update when version 1.4 is out.

### 2020-11-08 Project announcement
Today, the first auction shard chain rotation test is conducted. The 24th-ranked shard may not have as many tokens as the 25th, and it will be rotated by the system and cannot be produced (currently the 24th address of the testnet bidding shard a7kj1r4ugMGXko48f9WkkEv3ipa5CgVWZ holds 1.1 million coins)

### 2020-11-09 Project announcement
Smartx.net’s testnet version 1.4 was released. It mainly fixes k-bucket broadcasting, block pull, opener page file login, mining pool transfers to themselves, and broadcasting reaches the need to confirm issues, download address
https://github.com/mangodager/smartx-dotnet/releases/download/1.4/smartx-dotnet-alpha-1.4.zip

### 2020-11-12 Project announcement
Two third-party community mining pools have already supported smartx Zhitu PoW mining pool mining. Welcome to test. The addresses are jnpool.com and smartx.news. At present, the number of smartx PoW mining pools has reached 3

### 2020-11-13 Project announcement
Testnet 1.5.0 was released on 11-14. The core is to support smart contracts and contract template stores. At that time, testnet data will be cleaned up. Users are requested to be prepared.

### 2020-11-14 Project announcement
The smartx-dotnet testnet 1.5.2 smart contract version is coming as scheduled. It hits hard and surpasses user expectations. It supports one-click smart contract and contract template. It has been updated and downloaded on github.
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

### 2020-11-14 Project announcement
The smartx-dotnet testnet 1.5.2 smart contract version is coming as scheduled. It hits hard and surpasses user expectations. It supports one-click smart contract and contract template. It has been updated and downloaded on github.
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2X-dotnet.1.5.2.zip

### 2020-11-16 Project announcement
The smartx-dotnet testnet 1.5.2a version is released, which optimizes the block pulling speed and p2p broadcast of the smart contract version, and has been updated synchronously on github download
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.2aX-dotnet.1.5.2a.zip

### 2020-11-21 Project announcement
Most of the problems encountered by ordinary users in issuing tokens are summarized in 3 points:
1. Permission problem, it is useless to run the CMD window with administrator permissions or right-click the administrator permissions to run the SmartX-dotnet.bat batch file
2. The synchronization is not completed, check the current altitude:
https://pool.smartx.one/#/
The local altitude must run to the latest altitude to display your issued tokens
3. When issuing tokens, the nonce value must be the default and cannot be modified

### 2020-11-27 Project announcement
At present, the height of the smartx-dotnet test network has stopped growing to fix bugs. This is a normal situation during the test phase, please let users know.

### 2020-11-27 Project announcement
Fix bugs, update smarx-dotnet 1.5.2b version, apply the 1.5.2 patch directly on the basis of 1.5.2a, and fix the problem of high CPU usage during block production.

### 2020-11-28 Project announcement
The smartx-dotnet test network is updated to version 1.5.3. More than 120 bidding nodes that do not produce blocks will automatically exit the verification network, and they need to be added manually in the future.

### 2020-11-28 Project announcement
There are a lot of version updates in the past two days. If users find any problems, please feedback in the team forum, https://bbs.smartx.one/
In addition, the latest version 1.5.3a test version is officially released, and has been synchronized with github download https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3aX-dotnet-1.5.3a.zip
1. Optimized the logic of synchronous pull block
2. If more than 130 highly bid verification shards do not produce blocks, they will automatically exit the rules list and need to be added manually

### 2020-12-03 Project announcement
smartx-dotnet has upgraded to the latest version 1.5.3b, which is compatible with version 1.5.3a. If the pulling speed is not slow, you don’t need to update it. Main optimization:
1. Multi-coroutine pull block, the block pull speed increases several times
2. Optimize memory, CPU usage
3. Abnormal error report due to lock
Open github download
https://github.com/mangodager/smartx-dotnet/releases/download/1.5.3b/smartx-dotnet-1.5.3b.zip

### 2020-12-07 Project announcement
The testnet 1.5.3b version is testing other use cases, and the team PoW mining pool pool.smartx.one is temporarily stopped. Please know!

### 2020-12-09 Project announcement
Testnet 1.5.3b has fixed some bugs. A few days ago, the testnet.smartx.one website service node 138 node was stopped, and it has been restarted today. Users can currently access normally.

### 2020-12-11 Project announcement
smartx-dotnet-1.5.3c version officially released
1. This is a more important version
2. The version can be merged into the main chain due to the isolated chain generated by selfish mining or network split
3. Improve the request speed of the internally managed http interface

### 2020-12-16 Project announcement
The latest Coral version mainnet 1.2.5 version is officially released
1. Support new UI styles
2. Support using private key to import and transfer

### 2020-12-22 Project announcement
The smartx-dotnet-1.5.3c version has completed the fork chain mergechain test and the PoW mining pool rotation test. The normal testnet mining test has been opened. The official mining ip/address and the mining pool website address have not changed, please let us know.

### 2020-12-23 Project announcement
The smartx-dotnet-1.5.3c version has launched the latest block explorer and web wallet, address: https://testnet.smartx.one/browser and https://testnet.smartx.one
Please know

### 2020-12-30 Project announcement
The third-party wallet has supported Zhitu, the official website is:
https://hebe.cc/sat/sat.htmlat.html

### 2021-01-06 Project announcement
smartx-dotnet-1.5.3f version released, fix consensus bug

### 2021-01-11 Project announcement
The smartx-dotnet-1.5.3g version was released, and the testnet reconciliation function was updated. Start testnet reconciliation and transaction data verification to prepare for the official launch of the mainnet.

### 2021-01-19 Project Announcement 1
The smartx-dotnet-2.0 version is released, and the "shard chain" contract pledges liquidity mining, decentralized pledge and access at any time.

### 2021-01-19 Project Announcement 2
The smartx-dotnet-2.0 version is released. If there is a test coin, you can use the test coin to conduct mortgage liquidity mining at the node http://pos.smartx.one:8101.

### 2021-01-20 Project announcement
The smartx-dotnet-2.0.1 version is released, and the 19th data has been cleaned up. Modified the reward method of pos and pow. The handling fee of pos is increased by 2%, then the crowdfunding is adjusted to 300w, the block generation of pos is adjusted to 12, and the pow is adjusted to 72
If there is a test coin, you can use the test coin to conduct mortgage liquidity mining at http://pos.smartx.one:8101 node.
Pledge contract mining tutorial: https://bbs.smartx.one/t/topic/548

### 2021-01-21 Project announcement
The smartx-dotnet-2.0.2 version is released, which fixes some bugs of the satswap exchange, and the test data has been cleaned up. Please note.

### 2021-03-25 Project announcement
1. Improve the reconciliation function of Zhitu token
2. Token display in block browser
3. Token transfer transaction sorting function of block browser

## 2021-04-02 Project announcement
1. Test new dynamic satdag mining algorithm
2. The transfer function of pledge contract is added, and the transfer contract needs handling fee
3. Automatic beruler
4. POS node limits that a pledge contract must be created to become a block out node
5. Contract storage optimization
6. Add lock in contract function
7. Increase default transfer fee by 0.002 sat
8. Optimization of pow power statistics of the whole network and POW algorithm statistics of single machine
9. Add contract reconciliation function
10. It takes 100 sats to create a trading pair

## 2021-04-07 Project announcement
1. Optimizing contract calls by adding cache
2. Modify command start line

## 2021-08-02 Version 3.2.0d Project Announcement
1. Parameter adjustment of the official version of PoW and PoS
2. Add the minimum computing power submission limit
3. The PoW mining algorithm is modified to randomX
4. Fix RandomX memory leak problem
5. 3.2.0d multi-threaded version
