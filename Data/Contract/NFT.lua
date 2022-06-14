
Storages = {}

function create(_name,_symbol)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.totalSupply  = 0;

        lualib.TransferEvent(sender, "", "create: " .. addressThis);
	end
end


--返回ERC20代币的名字
function name()
	return Storages.name;
end

--返回代币的简称
function symbol()
	return Storages.symbol;
end

--从代币合约的调用者地址上转移 _value的数量token到的地址 _to
function transfer(_to, _tokenId)
	lualib.Assert( sender ~= _to , "transfer: sender == _to" );
	lualib.Assert( ownerOf(_tokenId) == sender , "transfer: non owner" );

	-- sender
	local value_sender = luaDB.GetValue("ownerTokens",sender);
	if value_sender == nil or value_sender == "" then
		value_sender = {};
	end

	--print(rapidjson.encode(value_sender))
	for i=1,#value_sender do
		if value_sender[i] == _tokenId then
			table.remove(value_sender,i);
			break;
		end
	end
	--print(rapidjson.encode(value_sender))

	luaDB.SetValue("ownerTokens",sender,value_sender);

	-- to
	local value_to = luaDB.GetValue("ownerTokens",_to);
	if value_to == nil or value_to == "" then
		value_to = {};
	end
	table.insert(value_to,_tokenId);
	lualib.Assert( #value_to <= 1024 , "out limit 1024" );
	luaDB.SetValue("ownerTokens",_to,value_to);

	luaDB.SetValue("tokenOwners",_tokenId,_to);

	lualib.TransferEvent(sender, _to, _tokenId);
	return true;
end

function mint(_to, _tokenId, _calldata)
	lualib.Assert( sender == Storages.publisher, "mint: unauthorized" );
	lualib.Assert( _to ~= nil and _to ~= "", "_to == nil" );
	lualib.Assert( _tokenId ~= nil and _tokenId ~= "", "mint: _tokenId == nil" );
	lualib.Assert( tokenMetaData(_tokenId) == nil, "mint: Already exists" );

	luaDB.SetValue("tokenOwners",_tokenId,_to);
	luaDB.SetValue("tokenLinks",_tokenId,_calldata);

	local value_sender = luaDB.GetValue("ownerTokens",_to);
	if value_sender == nil or value_sender == "" then
		value_sender = {};
	end

	table.insert(value_sender,_tokenId);

	luaDB.SetValue("ownerTokens",_to,value_sender);

	Storages.totalSupply = Storages.totalSupply + 1;
end

-- 获取归属
function ownerOf(_tokenId)
	return luaDB.GetValue("tokenOwners",_tokenId);
end

-- 获取元数据
function tokenMetaData(_tokenId)
	return luaDB.GetValue("tokenLinks",_tokenId);
end

--返回token的总供应量
function totalSupply(_address)
	if _address ~= nil and _address ~= "" then
		local value_sender = luaDB.GetValue("ownerTokens",_address);
		if value_sender == nil or value_sender == "" then
			value_sender = {};
		end
		return #value_sender;
	end
	return Storages.totalSupply;
end

function tokenOwnersAll(_address)
	local value_address = luaDB.GetValue("ownerTokens",_address);
	if value_address == nil or value_address == "" then
		value_address = {};
	end
	return rapidjson.encode(value_address);
end










