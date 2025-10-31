# What's going on here?

The MQR project is driven by instruction sets.

These are text files which declaratively describe how to interact with the mainframe.

The text files were originally specified in XML.

This led to a lot of pain in serialising them. When remaking MQR, we decided to use JSON instead.

The types in the 'Legacy' folder are direct transliterations of the XML schemas. That's why they're all stringly-typed - we wanted to be very permissive.

The mappers in this folder take you from the legacy types to the new types.

The mappers are mostly boilerplate. But they do encode some logic, namely around sentinel values the XML files used.

For instance, they often used '-1' as a 'do everything' value. We now use `null` for this.

They similarly used negative durations for infinity, 01/01/0001 for dates, etc.

In the new models, we tried to make use of the type system, so we filter out these junk values.