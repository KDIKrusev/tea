// Types the calculation works with internally and the configuration it reads. Imported globally so
// that a per-file `using` block stays about the file's own subject.
//
// The distinction these namespaces encode:
//   Models/          → the WIRE CONTRACT. Anything here is serialized to the client; changing a
//                      property name or type is a breaking change for cl/.
//   Models/Domain/   → internal calculation types. Never serialized, free to change.
//   Models/Settings/ → appsettings.json-bound configuration.
global using KSailCalc.Api.Models.Domain;
global using KSailCalc.Api.Models.Settings;
