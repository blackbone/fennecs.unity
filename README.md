# Fennecs Unity

This is a package which allow to use [fennecs](https://fennecs.tech/) with unity utilizing Job system as main running power.

><details>
>    <summary>Disclamer</summary>
>
>    According to original licence it's MIT so evend if `Unity dumb` i managed to make it work but with some [changes](#changes).
></details>
 
## Docs

Refer [original docs](https://fennecs.tech/docs/) to know how to work with this ECS.

Jobs workflow described in [section](#jobs-workflow) below.

## Installation

add to manifest.json
```json
"com.blackbone.fennecs-unity" : "https://github.com/blackbone/fennecs.unity.git"
```
also release tags can be specified like this
```json
"com.blackbone.fennecs-unity" : "https://github.com/blackbone/fennecs.unity.git#0.2.0-beta"
```

## Usage and code samples

### Jobs workflow

#### Jobs

Unity jobs workflow is pretty straightforward and implemented as extension methods for `Query` named as `JobFor`.

Same as original ones they support 1-5 component types with 2 overloads - one with uniform and one without.

> [!IMPORTANT]
> To get maximum perfomance possible under the hood it uses raw pointers and assumes that you'll not make structural changes while jobs are running. Just keep this in mind and from my side i'll place some guards and checks to keep it as safe as possible.

#### Rendering and other access

In some cases there's a need to get just access to components in a batch without manipulating them. For example when you need to render with instancing or BRG you need to access to Matrix and Mesh \ Material components.

There's new method called `CrossJoin` added for that.

It utilizes fennece's own `Archetype.CrossJoin` and expose raw data batches so you can iterate them and process in batch.

### Restrictions and notes

There's a couple of restriction because of Unity's nature:

1. Components are limited to `struct` because Jobs are only capable to process structs and managed types are not supported (but you can still use navive `For` and `Job`)

2. For perfomance considerations try to keep all processing methods as `static` while using `JobFor`. It's required because of pinning pointers magic under the hood and will benefit a bit.

### Code samples

<details>
<summary>Simple increment</summary>

```csharp
using fennecs;

class Sample
{
    private struct Component { public int value; }
    
    private readonly World world = new();
    private readonly Query<Component> components;

    public Sample() {
        var c = new Component { value = 0 };
        components = world.Query<Component>().Build();
        for (var i = 0; i < 128; i++)
            world.Spawn().Add(c);
    }

    public void Update()
    {
        components.For((ref Component c) => c.value++); // will run on main thread
        components.Job((ref Component c) => c.value++); // will run on thread pool
        components.JobFor((ref Component c) => c.value++); // will run with jobs
    }
}
```
</details>

<details>
<summary>Two components</summary>

```csharp
using fennecs;

class Sample
{
    private struct Component1 { public int value; }
    private struct Component2 { public int value; }
    
    private readonly World world = new();
    private readonly Query<Component1, Component2> cross;

    public Sample()
    {
        var c1 = new Component1 { value = 0 };
        var c2 = new Component2 { value = 1 };
        cross = world.Query<Component1, Component2>().Build();
        for (var i = 0; i < 128; i++)
            world.Spawn().Add(c1).Add(c2);
    }

    public void Update()
    {
        cross.For((ref Component1 c1, ref Component2 c2) => c1.value += c2.value); // will run on main thread
        cross.Job((ref Component1 c1, ref Component2 c2) => c1.value += c2.value); // will run on thread pool
        cross.JobFor((ref Component1 c1, ref Component2 c2) => c1.value += c2.value); // will run with jobs
    }
}
```
</details>

<details>
<summary>Two components with uniform</summary>

```csharp
using fennecs;

class Sample
{
    private struct Component1 { public int value; }
    private struct Component2 { public int value; }
    
    private struct Uniform
    {
        public int value;
    }
        
    private readonly World world = new();
    private readonly Query<Component1, Component2> cross;

    public Sample()
    {
        var c1 = new Component1 { value = 0 };
        var c2 = new Component2 { value = 1 };
        cross = world.Query<Component1, Component2>().Build();
        for (var i = 0; i < 128; i++)
            world.Spawn().Add(c1).Add(c2);
    }

    public void Update()
    {
        var uniform = new Uniform { value = 2 };
        cross.For((ref Component1 c1, ref Component2 c2, Uniform u) => c1.value += c2.value * u.value, uniform); // will run on main thread
        cross.Job((ref Component1 c1, ref Component2 c2, Uniform u) => c1.value += c2.value * u.value, uniform); // will run on thread pool
        cross.JobFor((ref Component1 c1, ref Component2 c2, Uniform u) => c1.value += c2.value * u.value, uniform); // will run with jobs
    }
}
```
</details>

<details>
<summary>Instanced rendering</summary>

```csharp
using fennecs;
using UnityEngine;

class Sample : MonoBehaviour
{
    private struct Position { public Vector3 value; }
    private struct Velocity { public Vector3 value; }

    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;
    
    private readonly World world = new();
    private readonly Query<Position, Velocity, Quaternion> rotationQuery;
    private readonly Query<Position, Quaternion, Matrix4x4> transformUpdateQuery;
    private readonly Query<Matrix4x4> transformQuery;

    public Sample()
    {
        rotationQuery = world.Query<Position, Velocity, Quaternion>().Build();
        transformUpdateQuery = world.Query<Position, Quaternion, Matrix4x4>().Build();
        transformQuery = world.Query<Matrix4x4>().Build();
    }

    public void Update()
    {
        rotationQuery.JobFor(UpdateRotation);
        transformUpdateQuery.JobFor(UpdateTransform);
        transformQuery.Cross(RenderBatch);
    }

    private static void UpdateRotation(ref Position position, ref Velocity velocity, ref Quaternion rotation)
        => rotation = Quaternion.FromToRotation(position.value, position.value + velocity.value);
    
    private static void UpdateTransform(ref Position position, ref Quaternion rotation, ref Matrix4x4 transform)
        => transform.SetTRS(position.value, rotation, Vector3.one);

    private void RenderBatch(Matrix4x4[] transforms, int count)
        => Graphics.DrawMeshInstanced(mesh, 0, material, transforms, count);
}
```
</details>
 
## Changes <sub>*compared to original code*</sub>

Because of nature of original project it's not possible to use it 'as is' with Unity so i managed to make it a bit different way.

General idea is to have as close as much original code so it's done by compiling it to a `dll` and deliver as managed plugin inside package.
It made with custom shell script you can see [here](https://github.com/blackbone/fennecs.unity/blob/main/sync.sh).

In short - it downloads original repository, copies source files to prepared project, makes necessary compatibility changes with `sed` and builds to `Runtime/Plugins/fennEcs` folder.

Resulting dll is **netstandard2.1** dll built with **.Net 8** which allows to support C# 12 features but not all of them.

Three problems have been solved and there's in short which and how:

* **API Compartibility** - in some places new versions of api used like short `Array.Clear` semantics, `Random.Shared`, co-variant overrides or `ThreadPool.UnsafeQueueUserWorkItem` so that kind of stuff replaced with `sed` in sync script.

* **SDK Compatibility** - because **netstandard** is quite old for now and missing some used features such as *Immutable Collections* used in original source part of newest dotnet was vendored inside project.
This also helped to support such features as required fields for example.
In future (i hope) i'll get rid of this when Unity finally updates .NET backend.

* **Unity integration access** - it was necessary to expose some members to use in unity integrationso `AssertNotDisposed()` method and `Archetypes`, `World` fields were exposed to `protected internal` and internals are now also visible to `Fennecs.Unity` assembly which is Runtime assembly for this project.
