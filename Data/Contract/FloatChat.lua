
Storages = {};
Storages.alloweds = {};

function create(_name,_symbol,_tokenA,_gas,_gaeAddress)
	if Storages.publisher==nil then
		Storages.publisher = sender;
		Storages.name    = _name;
		Storages.symbol  = _symbol;
		Storages.tokenA  = _tokenA;
		Storages.gas     = _gas;
		Storages.gaeAddress = _gaeAddress;

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

function setGas(_gas,_gaeAddress)
	if Storages.publisher==sender then
		_gas = biglib.Abs(_gas)
		Storages.gas = _gas;
		Storages.gaeAddress = _gaeAddress;
	end
end

function chat(address)
	if biglib.Greater(Storages.gas,"0",false) and Storages.gaeAddress ~= "" and Storages.gaeAddress ~= nil then
		lualib.TransferToken(Storages.tokenA,sender,Storages.gaeAddress,Storages.gas);
	end
    lualib.TransferEvent(sender, address, "chat");

end



