
Storages = {}

function create(_name,_symbol)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.pairs   = {};
		Storages.Fee     = "100";

        lualib.TransferEvent(sender, "", "create: " .. addressThis);
	end
end

function pairCreated(tokenA,tokenB,amountInA,amountInB)
	--
	if Storages.gas ~= nil and Storages.gaeAddress ~= nil then
	    lualib.Transfer(sender, Storages.gaeAddress, Storages.gas);
	else
	    lualib.Transfer(sender, "dQWeCkqPqRKw9q6ehB8gTxvCVRQKa3pDN","100");
	end

	lualib.Assert( lualib.IsERC(tokenA) , "IDENTICAL_ADDRESSES A");
	lualib.Assert( lualib.IsERC(tokenB) , "IDENTICAL_ADDRESSES B");

	if lualib.StringCompare(tokenA , tokenB) < 0 then
		local temp = tokenA;
		local tokenA = tokenB;
		local tokenB = temp;
	end

	if Storages.pairs[tokenA] == nil then
		Storages.pairs[tokenA] = {};
	end

	lualib.Assert( tokenA ~= tokenB , "tokenA == tokenB");
	lualib.Assert( Storages.pairs[tokenA][tokenB] == nil , "PAIR_EXISTS");

	local data = string.format("create(\"SFPair\",\"SFP\",\"%s\",\"%s\",\"%s\",\"%s\",\"%s\")",tokenA,tokenB,amountInA,amountInB,addressThis);
    local pair = lualib.Create(data,"SatswapPair");
	lualib.Assert( pair ~= nil , "INSUFFICIENT_FACTORY_CREATE")

	Storages.pairs[tokenA][tokenB] = pair;

end

function getPairs()
	return rapidjson.encode(Storages.pairs);
end

function setGas(_gas,_gaeAddress)
	if Storages.publisher==sender then
		_gas = biglib.Abs(_gas)
		Storages.gas = _gas;
		Storages.gaeAddress = _gaeAddress;
	end
end




















