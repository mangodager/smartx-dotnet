
Storages = {}
Storages.Rules = {}

function Create()
	local Rule = {}
	Rule.Address = curAddress
	Rule.Start   = curHeight
	Rule.End     = -1
	table.insert(Storages.Rules,Rule)
	Storages.Address = curAddress;
end

function Sort(a,b)
	if a.Address == Storages.Address then
		return true
	end
	if b.Address == Storages.Address then
		return false
	end
	if bigint.Equal(a.Amount , b.Amount) then
		return lualib.StringCompare(a.Address , b.Address) > 0;
	end

	return bigint.Greater(a.Amount , b.Amount,false);
end

function Add()
	if bigint.Less(lualib.GetAmount(curAddress) , "10000000000") then
		return
	end

	-- is kickout
	local find = false
	for i = #Storages.Rules , 1 , -1 do
		if Storages.Rules[i].Address == curAddress then
			find = true
		end
	end

	-- else add
	if find == false then
		local Rule = {}
		Rule.Address = curAddress
		Rule.Start   = curHeight + 10
		Rule.End     = -1
		table.insert(Storages.Rules,Rule)
	end

end


function Update()
	-- delete height out
	for i = #Storages.Rules , 1 , -1 do
		if Storages.Rules[i].End ~= -1 and Storages.Rules[i].End < curHeight then
			table.remove(Storages.Rules,i)
		end
	end

	-- updata and check amount
	for i=1,#Storages.Rules do
		Storages.Rules[i].Amount = lualib.GetAmount(Storages.Rules[i].Address)
	end

	table.sort(Storages.Rules,Sort)

	for i=1,24 do
		if Storages.Rules[i] == nil then
			break;
		end
		if bigint.Less(Storages.Rules[i].Amount , "10000000000") and Storages.Rules[i].End == -1 and Storages.Rules[i].Address ~= Storages.Address then
			Storages.Rules[i].End = curHeight + 10
		end
		if bigint.Greater(Storages.Rules[i].Amount , "10000000000",true) and Storages.Rules[i].End ~= -1 then
			Storages.Rules[i].End = -1
		end
	end

	-- kickout the last
	for i=25,#Storages.Rules do
		if Storages.Rules[i].End == -1 and Storages.Rules[i].Address ~= Storages.Address then
			Storages.Rules[i].End = curHeight + 10
		end
	end


end



