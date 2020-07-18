MongODM
=========

## Overview

MongODM is a **ODM framework** (Object-Documental Mapping) developed for work with **MongoDB** and **Asp.NET** applications.

With MongODM you can concentrate on developing your application, without worring on how data is organized in documents. **Documental databases are very efficient**, but very often using them we have to concern about **how data is structured** at the application layer. MongODM creates a document-to-object mapping layer, solving this kind of problems.

Infact, the real powerful and different vision of documental DBs against SQL DBs resides into **data denormalization**. With denormalization we can optimize data structure for get all required information with a single document read, without join, but price of this efficiency is often paid with a more difficult to develop application layer. Indeed, often writing applications with documental DBs we can encounter these kind of issues:

- Related models are connected on our application domain, but different models are saved on different documents. Logical join operations have so to be performed on application layer instead of db layer using different queries
- Denormalization can optimize reading operations, but if requirements evolve, we could have to update the schema of all saved documents. Or even worse, we could have to decorate our application with explicit checks and conditions on data loaded
- To update a document is easy, but to update a document that has been denormalized into others can be really painful. We have to trace where denormalization has gone, and update every dependent document

For these reasons SQL databases are often still prefered to documentals, because data management from an application perspective is easier, even if documental can be more efficient. Or often documental DBs are used as a simple CRUD storage layer, where save only plain data without make any use of denormalization advantages.

**MongODM has the scope to solve these problems**, and its objective is to **bring efficiency of denormalized documental data also to complex applications**, without have to worry at all of how data is organized from an application layer!

Moreover, MongODM uses official [MongoDB's drivers for C#](https://github.com/mongodb/mongo-csharp-driver), thus inheriting all the powerful features developed for example about description of data serialization, and keeping compatibility with any new relase of MongoDB Server or MongoDB Atlas.

Here is a non exhaustive list of implemented features from MongODM:

- Create relation between documents, and fine configuration of serialized properties for document denormalization
- Transparent lazy loading for unloaded properties
- Manage repositories grouped in database contexts
- Automatic denormalization dependent documents update
- Work with asynchronous database maintenance tasks based on [Hangfire](https://www.hangfire.io/), or on your independent system
- Customizable database indexes
- Handle different versioned document schemas on same collections
- Configurable data migration between document versions
- Oriented to Dependency Injection for improve code testability
- Native integration with Asp.NET Core

**Disclaimer**: MongODM is still in a pre-beta phase, new features are going to be implemented, and current interface is still susceptible to heavy changes. At this stage active use in production is discouraged.

Package repositories
--------------------

You can get latest public releases from Nuget.org feed. Here you can see our [published packages](https://www.nuget.org/profiles/etherna).

If you'd like to work with latest internal releases, you can use our [custom feed](https://www.myget.org/F/etherna/api/v3/index.json) (NuGet V3) based on MyGet.

Documentation
-------------

For specific documentation on how to install and use MongODM, visit our [Wiki](https://github.com/Etherna/mongodm/wiki).

Issue reports
-------------

If you've discovered a bug, or have an idea for a new feature, please report it to our issue manager based on Jira https://etherna.atlassian.net/projects/MODM.

Detailed reports with stack traces, actual and expected behaviours are welcome.

Questions? Problems?
---------------------

For questions or problems please write an email to [info@etherna.io](mailto:info@etherna.io).
