# Synopsis

OSAMES ORM is a C# lightweight ORM based on ADO.NET.

It's developed with .NET 4.6.1. We are working on building it with Mono and .NET Core 1 too (not usable for now).

## Code Example

**Mapping:**

```xml
<orm:MappingObject property="IdCustomer" column="CustomerId"/>
```

**Template:**

```xml
<orm:Select name="BaseReadWhere">SELECT {0} FROM {1} WHERE {2} = {3}</orm:Select>
```

**C# code:**

```csharp
_config = ConfigurationLoader.Instance;
Customer customer = DbToolsSelects.SelectSingle<Customer>(new List<string> { "IdCustomer", "FirstName", "LastName" }, "BaseReadWhere",
    new List<string> { "City", "#" }, new List<object> { "Paris" }, _transaction);
```

## Motivation

This lightweight ORM aims at:

* having a maintainable SQL templates list and simple database to object mapping XML definition,
* allowing to quickly switch to another provider thanks to ADO.NET API
* providing robustness against connection pool starvation

## Installation

Compile the DLL and include it in your application, along with its configuration.

Check out [documentation](http://confluence.osames.org/pages/viewpage.action?pageId=26542093) (mostly in French, English translation coming now and then).

### .NET Core

You need .NET Core: <https://dotnet.github.io/>

#### Nuget Package

##### Windows

Download Nuget.exe on <https://dist.nuget.org/index.html>
Restore package: ```nuget.exe restore``` in project or solution folder. For more informations to restore needed packages, read Omnisharp output.

##### Linux/Osx

Coming soon.

### .NET Core

You need .NET Core: <https://dotnet.github.io/>

#### Nuget Package

##### Windows

Download Nuget.exe on <https://dist.nuget.org/index.html>
Restore package: ```nuget.exe restore``` in project or solution folder. For more informations to restore needed packages, read Omnisharp output.

##### Linux/Osx

Comming soon.

## API Reference

Check out documentation (see link in Installation above). A complete API reference will be provided soon.

## Tests

Unit tests were initially written using MS Tests, but only a few changes make them portable to nUnit/xUnit.
We are working on porting them to xUnit.

## Contributors

[Issue tracker](http://issues.osames.org/projects/ORM)

## License

GNU Affero General Public License version 3 or later.

<http://www.gnu.org/licenses/agpl-3.0.html>