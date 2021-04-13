
Storages = {}

function create(_name,_symbol)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;

        lualib.TransferEvent(sender, "", "create: " .. addressThis);
	end
end

function getPair(_owner)
	return luaDB.GetValue("pairs",_owner);
end

function approve(_to,tokenA,amountIn,_lockTime,_name)
	lualib.Assert( lualib.IsERC(tokenA) , "error: tokenA");

	local tokenC = luaDB.GetValue("pairs",_to);
	-- tokenC
	if tokenC == nil then
		local dataC  = string.format("create(\"LockPair\",\"LKP\"");
			  tokenC = lualib.Create(dataC,"LockPair");
		lualib.Assert( tokenC ~= nil , "error: pairCreated")

		luaDB.SetValue("pairs",_to,tokenC);
		luaDB.SetValue("pairs",tokenC,_to);

	end

	local dataA = string.format("approve(\"%s\",\"%s\",\"%s\",\"%s\",%s,\"%s\")",tokenA,sender,_to,amountIn,_lockTime,_name);
	local  relA = lualib.Call(tokenC,dataA);
	lualib.Assert( relA , "error: approve2" );

end

function retrieved(_lockHeight)
	local tokenC = luaDB.GetValue("pairs",sender);
	lualib.Assert( lualib.IsERC(tokenC) , "error: tokenC");

	local dataA = string.format("retrieved(\"%s\",%s)",sender,_lockHeight);
	local  relA = lualib.Call(tokenC,dataA);
	lualib.Assert( relA , "error: retrieved" );

end

function allowance(_to)
	local tokenC = luaDB.GetValue("pairs",_to);
	if lualib.IsERC(tokenC) == false then
		return nil;
	end
	return lualib.Call(tokenC,"allowance()");
end



