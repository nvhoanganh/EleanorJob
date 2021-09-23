# Eleanor Job

## How does it work

<img src="https://raw.githubusercontent.com/nvhoanganh/EleanorJob/master/diagram.svg">

## Rules
- Message (called Payload) flows throw series of `steps` (Rectangle)
- each `step` = Pure Function (including the big ForEach)
- each `step` has the same signature (same inputs, same output)
- each `step` has different required properties and has different implementation. For exampe
```json
{
  "type": "TRANSFORM",
  "jsonSchema": {
    "FirstName": "$payload.firstName",
    "LastName": "$payload.lastName"
  }
},
{
  "type": "CREATE_CRM_ENTITY",
  "destEntityName": "driver"
}
```
- output from 1 step is the input to the next step
- each step can access global `Vars` (create/update)
- developers can use this https://eleanorjob.surge.sh to create the JSON file with intellisense

## Try this
- `dotnet run -- example1.json mapper13.json` 