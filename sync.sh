#!/bin/bash

echo "> comaring tags..."
# get latest tag
SOURCE_TAG=$(curl -s https://api.github.com/repos/thygrrr/fennecs/tags | grep name | cut -d '"' -f 4 | head -n 1)
echo "origin at $SOURCE_TAG"
THIS_TAG=$(curl -s https://api.github.com/repos/blackbone/fennecs.unity/tags | grep name | cut -d '"' -f 4 | head -n 1)
echo "this at $THIS_TAG"

# if most recent tags equals - we're in sync
if [ "$SOURCE_TAG" = "$THIS_TAG" ]; then
    echo in sync, nothing to do
    exit 0
fi

echo "> loading main repo code..."
# load main repo code
curl -LJO https://github.com/thygrrr/fennecs/archive/refs/tags/$SOURCE_TAG.zip

echo "> removing old code..."
# remove previously cloned folder if exists
rm -rf ./fennecs

echo "> unpackingnew code..."
# unsip main repo code
unzip ./fennecs-$SOURCE_TAG.zip -d ./fennecs

echo "> removing archive..."
# remove archive
rm fennecs-$SOURCE_TAG.zip

echo "> making directories..."
# copy all necessary files, this will overwrite structure
mkdir -p ./src~
mkdir -p ./src~/fennecs
mkdir -p ./src~/fennecs/pools
cp -r ./fennecs/fennecs-$SOURCE_TAG/fennecs/pools/*.cs ./src~/fennecs/pools
cp -r ./fennecs/fennecs-$SOURCE_TAG/fennecs/*.cs ./src~/fennecs

echo "> sed-ing..."
# replace `ArgumentNullException.ThrowIfNull(nameof(item));` to `if (item == null) throw new ArgumentNullException(nameof(item));`
sed -i -e 's/ArgumentNullException.ThrowIfNull(nameof(item));/if (item == null) throw new ArgumentNullException(nameof(item));/g' ./src~/fennecs/pools/ReferenceStore.cs

# replace `Array.Clear(srcStorage)` word in Query.cs with `Array.Clear(srcStorage, 0, srcStorage.Length)` - this is because netstandard2.1 not supports short overload
sed -i -e 's/Array.Clear(srcStorage);/Array.Clear(srcStorage, 0, srcStorage.Length);/g' ./src~/fennecs/Archetype.cs
sed -i -e 's/ArgumentOutOfRangeException.ThrowIfGreaterThan(row, Count, nameof(row));/if (row >= Count) throw new ArgumentOutOfRangeException(nameof(row));/g' ./src~/fennecs/Archetype.cs
sed -i -e 's/ArgumentOutOfRangeException.ThrowIfNegative(capacity, nameof(capacity));/if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));/g' ./src~/fennecs/Archetype.cs
sed -i -e 's/ArgumentOutOfRangeException.ThrowIfNegative(length, nameof(length));/if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));/g' ./src~/fennecs/Archetype.cs
sed -i -e 's/ArgumentOutOfRangeException.ThrowIfLessThan(length, Count, nameof(length));/if (length < Count) throw new ArgumentOutOfRangeException(nameof(length));/g' ./src~/fennecs/Archetype.cs

# replace `return this[System.Random.Shared.Next(Count)];` word in Query.cs with `return this[utility.RandomImpl.Next(Count)];` - this is because netstandard2.1 not supports Random.Shared
sed -i -e 's/System.Random.Shared.Next(Count)/utility.RandomImpl.Next(Count)/g' ./src~/fennecs/Query.cs

# replace `override` word in QueryBuilder[n].cs with `new` - this is because netstandard2.1 not supports co-variant overloads
sed -i -e 's/override/new/g' ./src~/fennecs/QueryBuilder.cs
sed -i -e 's/public abstract Query Build();/public virtual Query Build() => throw new InvalidOperationException();/g' ./src~/fennecs/QueryBuilder.cs

# # replace `ThreadPool.UnsafeQueueUserWorkItem(job, true);` word in Query[n].cs with `ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);` - this is because netstandard2.1 not supports newest thread pool and i haven't managed how to make it work in unity
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query1.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query2.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query3.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query4.cs
sed -i -e 's/ThreadPool.UnsafeQueueUserWorkItem(job, true);/ThreadPool.UnsafeQueueUserWorkItem(_ => job.Execute(), true);/g' ./src~/fennecs/Query5.cs
sed -i -e 's/override/new/g' ./src~/fennecs/Query1.cs
sed -i -e 's/override/new/g' ./src~/fennecs/Query2.cs
sed -i -e 's/override/new/g' ./src~/fennecs/Query3.cs
sed -i -e 's/override/new/g' ./src~/fennecs/Query4.cs
sed -i -e 's/override/new/g' ./src~/fennecs/Query5.cs

# # modify Queue.cs so `World`, 'Archetypes' and `AssertNotDisposed()` will be accessible from 'Fennecs.Unity' assembly
sed -i -e 's/private protected void AssertNotDisposed()/protected internal void AssertNotDisposed()/g' ./src~/fennecs/Query.cs
sed -i -e 's/private protected readonly List<Archetype> Archetypes;/protected internal readonly List<Archetype> Archetypes;/g' ./src~/fennecs/Query.cs
sed -i -e 's/private protected readonly World World;/protected internal readonly World World;/g' ./src~/fennecs/Query.cs

echo "> sed-ing done. removing intermediates..."
# clear after sed - it keeps original files 
rm ./src~/fennecs/*.cs-e

echo "> removing cloned folder..."
# remove cloned folder
rm -rf ./fennecs

echo "> building dotnet..."
# build dll and copy to unity's runtime folder
dotnet publish -c Release -o ./Runtime/Plugins/fennEcs ./src~/fennecs-unity.csproj
rm ./Runtime/Plugins/fennEcs/fennecs-unity.deps.json