
Storages = {}

function create(_name,_symbol)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.pairs   = {};

        lualib.TransferEvent(sender, "", "create: " .. addressThis);
	end
end

function pairCreated(tokenA,tokenB,amountInA,amountInB)
	-- 100SAT Fee
    lualib.Transfer(sender, "dQWeCkqPqRKw9q6ehB8gTxvCVRQKa3pDN","100");

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






















