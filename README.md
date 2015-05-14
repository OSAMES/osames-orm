## Synopsis

OSAMES ORM is a C# lightweight ORM based on ADO.NET.

Developed with .NET 4.5 and also built with matching Mono version.

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

We want to provide a lightweight ORM, where you can:
* have a maintainable SQL templates list and simple XML database to object mapping
* quickly switch to another provider thanks to ADO.NET API
* have robustness against connection pool starvation

## Installation

Compile the DLL and include it in your application, along with its configuration.

Check out [documentation](http://confluence.osames.org/pages/viewpage.action?pageId=26542093) (in French for now, English version will follow).

## API Reference

Check out documentation (see link in Installation above). A complete API reference will be provided soon.

## Tests

We initially wrote unit tests using MS Tests, but only a few changes make them portable to NUnit.

## Contributors

[Issue tracker](http://issues.osames.org/browse/ORM-170?filter=-4&jql=project%20%3D%20ORM%20AND%20status%20in%20%28Open%2C%20%22In%20Progress%22%2C%20Reopened%2C%20Resolved%2C%20%22In%20Review%22%2C%20%22Waiting%20for%20Unit%20Test%22%2C%20%22In%20Unit%20Test%22%2C%20%22In%20analysis%22%2C%20Analyzed%2C%20%22Waiting%20for%20review%22%29%20ORDER%20BY%20createdDate%20DESC)

## License

GNU Affero General Public License version 3 or later.

http://www.gnu.org/licenses/agpl-3.0.html