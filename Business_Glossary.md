Instrument Reference Data Service

Business Overview and Explanation of the Data Model

1. Introduction — What is this project?
The Instrument Reference Data Service is an application used to store and provide basic information about financial instruments.
In the simplest terms, it is a central catalogue of financial instruments used by a bank.
You can think of it as a very sophisticated address book.
In a normal address book, one person may have:
    • a name and surname
    • a phone number
    • an email address
    • a home address
    • a workplace
In the Instrument Reference Data Service, instead of people we have financial instruments, and instead of phone numbers or addresses we store information such as:
    • instrument name
    • identifiers
    • asset class
    • sector
    • issuer
    • exchange
    • currency
    • status
    • last update date
    • change history
The project describes the service as the bank’s source of truth — a central and reliable source of financial instrument reference data. Trading, risk, compliance, reporting and settlement systems can all depend on it.

2. What is a financial instrument?
A financial instrument is a product or contract with financial value that can be bought, sold, held or settled.
    • shares / equities
    • bonds
    • currencies
    • derivatives
    • futures contracts
    • options
    • some commodity-related products
For example, Apple Inc. common stock is a specific financial instrument.
It may have:
    • name: Apple Inc.
    • asset class: EQUITY
    • sector: Technology
    • issuer: Apple Inc.
    • currency: USD
    • an ISIN
    • other market identifiers
    • market or exchange information
The Instrument Reference Data Service is not primarily a system for storing the current market price of a share.
Its main purpose is reference data — information answering the question:
“What is this instrument?”
rather than:
“How much does it cost right now?”

3. What does Reference Data mean?
Reference Data means relatively stable information that is reused by many other systems.
    • list of currencies
    • list of exchanges
    • list of sectors
    • instrument types
    • instrument identifiers
    • issuer information
For example, a trading system may receive a transaction saying:
10,000 units of an instrument with ISIN XYZ were purchased.
The ISIN alone does not tell the system everything.
The Reference Data Service may respond that the ISIN belongs to instrument X, that it is an equity, that it was issued by company Y, belongs to sector Z, is listed on exchange A and uses GBP.
This means downstream bank systems do not have to maintain their own separate and potentially inconsistent copies of the same information.

4. Why does a bank need such a system?
A large bank may deal with hundreds of thousands of financial instruments, with thousands of updates arriving every day from external data providers. Bank systems may also perform real-time lookups.
Without a central service, different systems could hold conflicting information about the same instrument.
System	Instrument classification
Trading	EQUITY
Risk	BOND
Reporting	EQUITY
Settlement	missing
Each system might believe that its own value is correct. The key problem is that nobody knows which version is the real one.
The Instrument Reference Data Service is designed to prevent that situation.
Instead of several systems maintaining several versions of the same data, there is one central database used by many systems.

5. What does “Source of Truth” mean?

A source of truth is one trusted and authoritative version of the data.
If trading, risk and reporting systems all ask: “What is the asset class of instrument X?”, they should all receive the same answer.
This is one of the most important business purposes of the project.
Reference data errors can spread into many downstream processes. A wrong asset-class classification may cause incorrect risk calculations, incorrect regulatory reporting or settlement problems.

6. Who uses the system?

In this type of application, the 'client' is often not a person. It can be another bank system.

Trading
Systems responsible for executing and handling transactions. They need to know exactly what instrument is being traded.

Risk
Systems that calculate the bank’s risk. They may need to know whether an instrument is an equity or a bond, which currency it uses and which sector it belongs to.

Compliance
Systems that check whether the bank’s activity complies with regulations and internal policies.

Reporting
Systems used to prepare internal and regulatory reports.

Settlement
Systems responsible for the final settlement of transactions.

7. Important financial terms used in the project

ISIN
ISIN stands for International Securities Identification Number.
It is an international identifier for securities. A useful analogy is that it works like a unique personal identification number for a financial instrument.
Example: GB00B10RZP78
ISIN is important, but the service should also support other identifier types.

CUSIP
CUSIP is an identification system used mainly in North American markets.
The same instrument may have an ISIN, CUSIP, Bloomberg ID, RIC and SEDOL.
Therefore, the system should not assume 'one instrument = one identifier'. It should support one instrument having multiple identifiers.

SEDOL
SEDOL stands for Stock Exchange Daily Official List. It is an identifier used mainly in the UK market.

RIC
RIC stands for Reuters Instrument Code. It is used in systems associated with Reuters/Refinitiv market data.

Bloomberg ID
An identifier used in Bloomberg systems. A data vendor may know an instrument by one identifier while a regulator or another bank system may use an ISIN. The service therefore has to map different identifiers to the same underlying instrument.

MIC
MIC stands for Market Identifier Code. It identifies a market or exchange.

LEI
LEI stands for Legal Entity Identifier. In simple terms, ISIN identifies an instrument, while LEI identifies a legal entity such as a company or institution.

8. Asset Class
Asset Class means the main category of a financial instrument.
    • EQUITY — shares
    • BOND — bonds
    • FX — foreign-exchange related instruments
    • COMMODITY — commodity-related instruments
    • DERIVATIVE — derivatives
Asset class matters because a bank may treat different instrument types in very different ways, especially in risk calculations.

9. Sector
Sector means the industry or business area associated with the instrument or its issuer.
    • Technology
    • Financial Services
    • Healthcare
    • Energy
    • Consumer Goods
Sector classification is important because a bank may be highly exposed to one industry 
even if it holds instruments issued by many different companies.

10. Issuer
An Issuer is the entity that issued the financial instrument.
    • a company
    • a bank
    • a government
    • another institution
For example, Apple can be the issuer of its shares, while a government can be the issuer of government bonds.

11. Exchange
The Exchange table describes the market on which an instrument is listed or associated.
    • exchange_id
    • mic_code
    • exchange_name
    • country
    • timezone
    • currency_id
An exchange has its own attributes that may be shared by many instruments. Instead of storing 'London Stock Exchange' thousands of times in the Instrument table, the database stores the exchange once and links instruments to it.

12. Currency
The Currency table represents currencies such as USD, GBP and EUR.
A single currency may be used by many instruments and can also be associated with multiple exchanges, which is why it is stored as a separate reference table.

13. Instrument status
The status field describes the current state of an instrument, for example ACTIVE or INACTIVE.

An instrument should normally remain in the database even when it is no longer active, because the bank may have traded, held or reported it in the past.
This is why the project uses a soft-delete concept: deactivate the instrument rather than physically deleting its historical record.

14. Effective date
effective_date means the date from which a value or state becomes valid.

In financial systems, it is often not enough to know what the current value is. We also need to know from when it applies.

15. Last updated and stale data

last_updated shows when the instrument data was last updated.

Stale data means data that may no longer be current.

The project proposes a stale_instruments_vw view for active instruments that have not been updated for 30 or more days.

Stale does not automatically mean wrong. It means the data is old enough that it should be checked.

16. Audit — change history

InstrumentAudit stores the history of changes made to an instrument.
    • audit_id
    • instrument_id
    • changed_at
    • changed_by
    • field_name
    • old_value
    • new_value
    • change_source

Example: if sector changes from Consumer Goods to Personal Care, the audit record can store which field changed, the old value, the new value, when the change happened, who made it and where the change came from.
This gives the bank traceability: it can reconstruct what happened to the data over time.

17. Why is audit so important?

In banking it is often not enough to say: 'This is the value currently stored in the database.'

You may also need to answer: 'Why did this value change?' and 'What was the value yesterday?'

Audit history supports internal controls, compliance, incident analysis, regulatory reviews, debugging and reconciliation between systems.

18. Understanding the ERD / Mermaid diagram

The central table in the model is Instrument, because the entire service exists to store and describe financial instruments.

The surrounding tables answer business questions about that instrument:
    • Who issued it? → Issuer
    • What type of instrument is it? → AssetClass
    • Which industry does it belong to? → Sector
    • Where is it listed? → Exchange
    • Which currency is associated with it? → Currency
    • How can it be identified? → InstrumentIdentifier
    • What kind of identifier is it? → IdentifierType
    • What changed in the past? → InstrumentAudit

19. Instrument — the heart of the application

The Instrument table contains the main record for each financial instrument.
    • instrument_id — internal technical identifier
    • name — instrument name
    • primary_isin — primary ISIN
    • asset_class_id — link to AssetClass
    • sector_id — link to Sector
    • exchange_id — link to Exchange
    • currency_id — link to Currency
    • issuer_id — link to Issuer
    • status — whether the instrument is active
    • effective_date — date from which the data is valid
    • last_updated — date/time of the latest update

20. PK and FK

PK — Primary Key
A Primary Key is a value that uniquely identifies a row in a table. For example, instrument_id may uniquely identify each Instrument record.

FK — Foreign Key
A Foreign Key links one table to another. For example, if Instrument.currency_id = 2 and Currency record 2 is GBP, the database understands that the instrument is associated with GBP.

21. Why not store 'GBP' directly in Instrument?
Technically, it would be possible, but it would cause duplication and inconsistent values such as GBP, gbp, Gbp, British Pound or Pound Sterling.
A separate Currency table provides one controlled set of allowed values. This is one of the basic ideas behind database normalization.

22. Instrument — AssetClass relationship
Each instrument belongs to a particular asset class, while one asset class can describe many instruments.
Business meaning: one AssetClass → many Instruments.

23. Why is AssetClass a separate table?
Because it is a shared business category. If the bank changes the description of EQUITY, it can change it once instead of updating thousands of Instrument rows.
It also improves data quality because only approved asset classes can be referenced.

24. Instrument — Sector relationship
One sector can contain many instruments. This allows the bank to analyse exposure by industry, for example Technology or Financial Services.

25. Instrument — Issuer relationship
One issuer may issue many instruments, such as shares and several bond series.
This is better than repeating the issuer name separately in every Instrument row.

26. Instrument — Currency relationship
An Instrument has a currency_id, so one currency can be used by many instruments. This can support analyses such as currency exposure.

27. Exchange — Currency relationship
Exchange also contains currency_id. This represents the link between an exchange and a controlled currency record rather than storing currency as free text.

28. Instrument — Exchange relationship
Instrument contains exchange_id. Business-wise, this answers the question: 'Which market or exchange is this instrument associated with?'
One exchange can be related to many instruments.

29. InstrumentIdentifier — one of the most important tables
InstrumentIdentifier contains:
    • identifier_id
    • instrument_id
    • id_type_id
    • identifier_value
    • effective_date
    • expiry_date
One instrument can have multiple identifiers, for example an ISIN, SEDOL, RIC and Bloomberg 
ID. All of them may refer to the same underlying instrument.

30. Why not keep ISIN, CUSIP, SEDOL, RIC and Bloomberg ID as separate columns?
That design would be less flexible. If a new identifier type was introduced in the future, the database schema and application code might need to be changed.
With IdentifierType and InstrumentIdentifier, a new type can be added as data rather than requiring a new column.

31. IdentifierType
IdentifierType describes the category of identifier, such as ISIN, CUSIP, SEDOL, RIC or BLOOMBERG_ID.

One identifier type can be used by many InstrumentIdentifier records.

32. Why do identifiers have effective_date and expiry_date?
Identifiers can have a period of validity. The system should know not only which identifier belongs to an instrument, but also when that identifier was valid.
This also allows historical information to be preserved.

33. InstrumentAudit relationship
One instrument can have many audit records. Each time a field is changed, another audit record can be added.
The Instrument table represents the current state, while InstrumentAudit represents the history.

34. How to read relationship symbols in the Mermaid diagram
The diagram uses Crow’s Foot style cardinality notation.
    • Crow’s foot = many
    • Circle = zero / optional
    • Vertical bar = one
    • || = exactly one
    • ○| = zero or one
    • |< = one or many
    • ○< = zero or many
The symbols do not show the direction of data flow. They show how many records can participate in a relationship.

35. The easiest way to read cardinality

Ask a question about one table, then look at the symbol next to the other table.
Example: 'How many Instruments can one Exchange have?' Look at the symbol next to Instrument. If it is ○<, the answer is zero or many.
Example: 'How many Currencies can one Exchange have?' Look at the symbol next to Currency. If it is ||, the answer is exactly one.

36. Business view of the complete model
The bank stores a financial instrument and describes it using shared reference tables for asset class, sector, issuer, exchange and currency. At the same time, it can assign multiple identifiers to that instrument and preserve a complete change history.

37. Why is this model good from a business perspective?
It separates three types of data:

37.1. Core instrument data
Instrument — answers: What is this instrument?

37.2. Reference / dictionary data
    • AssetClass
    • Sector
    • Currency
    • Exchange
    • Issuer
    • IdentifierType
These answer: Which standard business categories describe the instrument?

37.3. Dependent and historical data
    • InstrumentIdentifier
    • InstrumentAudit
These answer: Which identifiers does it have, and what is its change history?

38. Data Quality
One of the main goals of the project is to control data quality.
    • missing sector
    • stale data
    • expired identifiers
    • unknown exchange
For example, an EQUITY instrument with Sector = NULL should be flagged as a data-quality issue.

39. Why is Data Quality important?
If a bank has 100,000 instruments and only 1% of records are wrong, that still means 1,000 potentially incorrect instruments.
If trading, risk, reporting, compliance and settlement all consume those records, one upstream error can spread to many downstream systems.

40. Database views
A database view can be thought of as a saved query that presents data in a convenient form.
active_instruments_vw

Shows active instruments together with relevant reference data.
instruments_by_asset_class_vw

Shows or groups instruments by asset class.
stale_instruments_vw

Shows active instruments that have not been updated within the defined threshold, such as 30 days.

41. Stored Procedures
A Stored Procedure is a program stored and executed in the database.

upsert_instrument

Upsert comes from UPDATE + INSERT. If the instrument does not exist, insert it. If it already exists, update it. Changes should also create audit records.
deactivate_instrument

Marks an instrument as INACTIVE instead of physically deleting it, and records the operation.

get_by_identifier

Finds an instrument using one of its supported identifiers.

get_audit_history

Returns the complete change history of an instrument.

42. Why is an API needed?
A database alone is not enough. Other bank applications need a controlled way to communicate with the service.
API stands for Application Programming Interface.
A useful analogy is a waiter: the client does not enter the kitchen. It asks the waiter for something, the waiter passes the request to the kitchen and returns the result.
Bank system
    ↓
   API
    ↓
Java application
    ↓
Database

43. API example
GET /api/instruments/123
This means: return the instrument with internal ID 123.
The server may return information such as instrumentId, name, assetClass, currency and status.

44. Lookup by identifier
GET /api/instruments/lookup?isin=...
The application finds the identifier, determines which instrument it belongs to, loads the instrument data and returns the result.
The business goal is to support lookup using multiple identifier types, not only ISIN.

45. POST, GET, PUT and DELETE

GET

Retrieve information.

POST

Create a new resource.

PUT

Update an existing resource.

DELETE

In this project, deactivate an instrument using soft delete.

46. Logging
Logging means recording information about what the application is doing.
    • INFO — normal operation, e.g. Instrument created
    • WARN — a situation requiring attention, e.g. Stale instrument
    • ERROR — a failure or rejected operation, e.g. Duplicate ISIN

47. Data feed
A data feed is an automated stream or batch of data coming from an external source.
A bank does not manually enter hundreds of thousands of instruments. Reference data may arrive from external market-data providers.
External Vendor
      ↓
Data Feed
      ↓
Instrument Reference Data Service
      ↓
Bank systems

48. Batch processing and real-time lookup

Batch
A large set of data processed together, for example an evening update containing 10,000 records.

Real-time lookup
Another system sends a request and expects an immediate response, for example when a trader needs to identify an instrument before a transaction.

49. Frontend
The project also requires a simple browser-based user interface.
    • search for an instrument
    • display instrument details
    • display identifiers
    • show exchange and currency
    • show last-updated information
    • show stale or recently changed data
This means a business user does not need to know SQL. They can search by an identifier such as ISIN and view the result.

50. Typical end-to-end data flow
    1. A vendor sends data.
    2. The Instrument Reference Data Service receives it.
    3. The system validates the information.
    4. It checks whether the instrument already exists.
    5. If it does not exist, create it; if it exists, update it.
    6. Write the change history to InstrumentAudit.
    7. Run data-quality checks.
    8. Expose the data through the API.
    9. Trading, Risk, Compliance, Reporting and Settlement systems consume it.

51. Example of a complete instrument
Instrument: HSBC Holdings PLC
AssetClass: EQUITY
Sector: Financial Services
Issuer: HSBC Holdings PLC
Exchange: London Stock Exchange
Currency: GBP
Status: ACTIVE
Possible identifiers:
    • ISIN → GB...
    • SEDOL → ...
    • RIC → HSBA.L
    • Bloomberg ID → HSBA LN Equity
Possible history:
    • 01.08 — instrument added
    • 03.08 — sector changed
    • 05.08 — Bloomberg ID added
    • 10.08 — last_updated changed

52. How the model reflects the real business
Instrument → Issuer
Who issued it?
Instrument → AssetClass
What kind of instrument is it?
Instrument → Sector
Which industry is it associated with?
Instrument → Exchange
On which market is it listed?
Instrument → Currency
Which currency is it associated with?
Instrument → InstrumentIdentifier
How can we identify it?
InstrumentIdentifier → IdentifierType
What type of identifier is it?
Instrument → InstrumentAudit
What changed in its data in the past?

The key purpose of the ERD is to translate real-world business relationships into relationships between database tables.

53. Business Story 1 — A trader wants to buy an instrument for a client

An institutional client asks the bank to buy an instrument identified by a specific ISIN. The trader knows the ISIN but may not know all of the instrument details.
The trading system sends a lookup request to the Instrument Reference Data Service. The service finds the ISIN, identifies the instrument, retrieves its asset class, sector, issuer, exchange, currency and status, and returns a consistent result.

Business value: the trader can be confident that the correct instrument is being traded, and downstream systems receive the same classification.

54. Business Story 2 — A Risk Manager detects a portfolio-data problem

The bank holds thousands of instruments. A Risk Manager wants to determine how much of the portfolio is exposed to the Technology sector.
If a major instrument is incorrectly classified as Consumer Goods instead of Technology, the resulting risk report could be wrong.
Using InstrumentAudit, the analyst can see when the sector changed, what the old and new values were, who or what changed it, and which source supplied the change.

Business value: the system provides traceability, making it possible to investigate and correct data problems.

55. Business Story 3 — Compliance prepares a regulatory report

The bank must prepare a large regulatory transaction report. Different source systems may know the same instrument by different identifiers: Trading may use a Bloomberg ID, Settlement may use SEDOL, while the report requires ISIN.
The Instrument Reference Data Service maps the incoming identifier to the Instrument and then returns the required primary ISIN.
It can also check for expired identifiers, missing sector information, unknown exchanges or stale data.

Business value: Compliance receives consistent identification data regardless of which identifier the source system originally used.

56. Main business value of the project
One version of the truth
All systems use consistent reference data.
Data Quality
The system detects missing, stale or invalid information.
Auditability
Every important change can be traced.
Multiple identifiers
The same instrument can be found using different identifier types.
Integration
The API allows many other applications to use the same service.
Regulatory support
Standard identifiers and historical traceability support regulatory processes.
Scalability
The architecture can handle large numbers of instruments and updates.

57. 30-second summary for a non-technical audience
The Instrument Reference Data Service is the bank’s central knowledge base for financial instruments.
It stores what an instrument is, who issued it, which category and sector it belongs to, where it is traded, which currency it uses, which identifiers it has, whether its data is current and what changed in the past.
Trading, Risk, Compliance, Reporting and Settlement systems can retrieve this information through an API.
The main goal is to provide consistent, current and traceable financial-instrument reference data across the bank.

58. One-sentence business summary
The Instrument Reference Data Service is a central source of trusted financial-instrument reference data that enables consistent identification, classification, data-quality control and change tracking, so that Trading, Risk, Compliance, Reporting and Settlement systems can all use the same version of the data.

59. The easiest mental model for the diagram
If you want to remember the diagram without memorising every table, think of Instrument as the centre of the model.
                    WHAT IS IT?
                        ↓
                   AssetClass

WHO ISSUED IT? → Instrument ← WHICH INDUSTRY?
     Issuer          ↑             Sector
                     |
              WHERE? / IN WHAT?
              Exchange / Currency
                     |
              HOW DO WE FIND IT?
                     ↓
          InstrumentIdentifier
                     ↓
              IdentifierType
                     |
              WHAT CHANGED?
                     ↓
             InstrumentAudit
Instrument is the centre of the model. The other tables answer business questions about that instrument.