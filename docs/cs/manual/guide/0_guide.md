# Introductory Guide {#guide}

This manual guide will cover with the greatest possible detail everything you need to know about the creation of SOS Modules or plug-ins for the mod **S.O.S - Standard Operations Schematics**.

## Contents List

1. @subpage getting_started - Here the most basic things are explained, like the way to create a mod, requirements, and it includes a code example both in C# and Lua of how to develop with our SDK.
2. @subpage sos_modules - This page defines what we consider an SOS Module (or SOS plug-in), and afterwards each type of admitted Module is explained and how you can implement your own modules in a vanilla way.
3. @subpage registration - Registering your module for the first time is very simple, this page explains in detail each form that currently exists to register a module.
4. @subpage data_sharing - For an effective communication between mods in a decentralized way, here is explained the canonical form to hook your delegates as hooks for a predictable and ordered execution right when you need it, besides also how to save shared data for everyone in a simple and thread-safe way.
5. @subpage ui_comps - To release all the power of the SDK, this exposes several custom GUI components and several helpers to make the modder's life easier, this section is in charge of explaining all the most important new members that you should know.

> Before any doubt, we recommend you to join the [official Discord](https://discord.gg/pjZ3SQjgM) of SOS and ask for help.

**Recomended to start:** [**Getting Started**](getting_started.html)
