#!/bin/bash

# # get latest tag
# SOURCE_TAG=$(curl -s https://api.github.com/repos/thygrrr/fennecs/tags | grep name | cut -d '"' -f 4 | head -n 1)
# echo "origin at $SOURCE_TAG"
# THIS_TAG=$(curl -s https://api.github.com/repos/blackbone/fennecs.unity/tags | grep name | cut -d '"' -f 4 | head -n 1)
# echo "this at $THIS_TAG"

# # if most recent tags equals - we're in sync
# if [ "$SOURCE_TAG" = "$THIS_TAG" ]; then
#     echo in sync, nothing to do
#     exit 0
# fi

echo synchronizing...
# load main repo code
curl -LJO https://github.com/thygrrr/fennecs/archive/refs/heads/main.zip

# remove previously cloned folder if exists
rm -rf ./fennecs

# unsip main repo code
unzip ./fennecs-main.zip -d ./fennecs

# remove archive
rm fennecs-main.zip

# copy all necessary files, this will overwrite structure
mkdir -p ./src~
mkdir -p ./src~/fennecs
mkdir -p ./src~/fennecs/pools
cp -r ./fennecs/fennecs-main/fennecs/pools/*.cs ./src~/fennecs/pools
cp -r ./fennecs/fennecs-main/fennecs/*.cs ./src~/fennecs

# replace `Array.Clear(srcStorage)` word in Query.cs with `Array.Clear(srcStorage, 0, srcStorage.Length)` - this is because netstandard2.1 not supports short overload
sed -i -e 's/Array.Clear(srcStorage);/Array.Clear(srcStorage, 0, srcStorage.Length);/g' ./src~/fennecs/Archetype.cs

# replace `return this[System.Random.Shared.Next(Count)];` word in Query.cs with `return this[utility.RandomImpl.Next(Count)];` - this is because netstandard2.1 not supports Random.Shared
sed -i -e 's/System.Random.Shared.Next(Count)/utility.RandomImpl.Next(Count)/g' ./src~/fennecs/Query.cs

# replace `override` word in QueryBuilder[n].cs with `new` - this is because netstandard2.1 not supports co-variant overloads
sed -i -e 's/override/new/g' ./src~/fennecs/QueryBuilder.cs

# # replace `ThreadPool.UnsafeQueueUserWorkItem(job, true);` word in Query[n].cs with `ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);` - this is because netstandard2.1 not supports newest thread pool and i haven't managed how to make it work in unity
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query1.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query2.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query3.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query4.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query5.cs

# # modify Queue.cs so `World`, 'Archetypes' and `AssertNotDisposed()` will be accessible from 'Fennecs.Unity' assembly
sed -i -e 's/protected void AssertNotDisposed()/protected internal void AssertNotDisposed()/g' ./src~/fennecs/Query.cs
sed -i -e 's/protected readonly List<Archetype> Archetypes;/protected internal readonly List<Archetype> Archetypes;/g' ./src~/fennecs/Query.cs
sed -i -e 's/private protected readonly World World;/protected internal readonly World World;/g' ./src~/fennecs/Query.cs

# clear after sed - it keeps original files 
rm ./src~/fennecs/*.cs-e

# remove cloned folder
rm -rf ./fennecs

# build dll and copy to unity's runtime folder
dotnet publish -c Release -o ./Runtime/Plugins/fennEcs ./src~/fennecs-unity.csproj
rm ./Runtime/Plugins/fennEcs/fennecs-unity.deps.json