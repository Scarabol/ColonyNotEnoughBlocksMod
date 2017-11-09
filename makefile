# important variables
modname = NotEnoughBlocks
version = 2.1

moddir = Scarabol/$(modname)
zipname = Colony$(modname)Mod-$(version)-mods.zip
dllname = $(modname).dll

#
# actual build targets
#

default:
	mcs /target:library -r:../../../../colonyserver_Data/Managed/Assembly-CSharp.dll -r:../../Pipliz/APIProvider/APIProvider.dll -r:../../../../colonyserver_Data/Managed/UnityEngine.dll -out:"$(dllname)" -sdk:2 src/*.cs

clean:
	rm -f "$(dllname)"

all: clean default

release: default
	rm -f "$(zipname)"
	cd ../../ && zip -r "$(moddir)/$(zipname)" "$(moddir)/modInfo.json" "$(moddir)/$(dllname)" "$(moddir)/blocks/examples"

client: default
	cd ../../../../ && ./colonyclient.x86_64

server: default
	cd ../../../../ && ./colonyserver.x86_64

