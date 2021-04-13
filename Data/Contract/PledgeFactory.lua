
Storages = {}

function create(_name,_symbol)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.feeTo   = sender;
		Storages.feeToSetter = "0.0001";

        lualib.TransferEvent(sender, "", "create: " .. addressThis);
	end
end

function pairCreated(tokenA,amountInA)
	lualib.Assert( lualib.IsERC(tokenA) , "IDENTICAL_ADDRESSES 1");
	lualib.Assert( biglib.Greater(amountInA , "100000", true) , "PledgeFactory:100000" );

	local pairs_sender = luaDB.GetValue("pairs",sender);
	lualib.Assert( pairs_sender == nil , "PAIR_EXISTS");

	-- tokenC
	local  dataC = string.format("create(\"PledgeLock\",\"EPL\",\"%s\")",tokenA);
    local tokenC = lualib.Create(dataC,"PledgeLock");
	lualib.Assert( tokenC ~= nil , "INSUFFICIENT_FACTORY_CREATE1")

	local data = string.format("create(\"PFPair\",\"PFP\",\"%s\",\"%s\",\"%s\",\"%s\")",tokenA,amountInA,addressThis,tokenC);
    local pair = lualib.Create(data,"PledgePair");
	lualib.Assert( pair ~= nil , "INSUFFICIENT_FACTORY_CREATE3")

	luaDB.SetValue("pairs",sender,pair);
	luaDB.SetValue("pairs",pair,sender);

end

function getPair(_owner)
	local pairs_owner = luaDB.GetValue("pairs",_owner);
	return pairs_owner;
end

function getFeeToSetter()
	return "dQWeCkqPqRKw9q6ehB8gTxvCVRQKa3pDN","0.0001";
	--return Storages.feeTo,"0.0001";
end











