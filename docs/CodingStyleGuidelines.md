# Coding Style Guidelines

* In general, we prefer verbose code (even if it's longer) than short & clever code. This means: we dislike short variable names (if there's some ambiguity on what your variable represents, then choose a longer and more descriptive name).

    Example (bad):
    ```csharp
    var d = GetDistance();
    var ns = GetNodes();
    ```

    Improved code:
    ```csharp
    var distanceInMeters = GetDistance();
    var availableNodes = GetNodes();
    ```

* Group import declarations (e.g. `open` in F# and `using` in C#) in three buckets: the first group for the namespaces that come from the base class libraries; second group for external libraries (e.g. nuget packages); last for the namespaces that belong to the source code that lives in the same repository.

    Example (bad):
    ```csharp
    using MyProject.Services;
    using System;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using MyProject.Models;
    ```

    Improved code:
    ```csharp
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using MyProject.Models;
    using MyProject.Services;
    ```

* In languages where writing `this.` is optional, prefer making it mandatory, as this makes the code more readable (even if more verbose).

    Example (bad):
    ```csharp
    public class Foo
    {
        private int bar;

        public void DoSomething()
        {
            bar = 42;
        }
    }
    ```

    Improved code:
    ```csharp
    public class Foo
    {
        private int bar;

        public void DoSomething()
        {
            this.bar = 42;
        }
    }
    ```

* Respect style that aims for future maintainability:

    * Always use indentation when you can (for readability).

    Example (with too cramped style):
    ```typescript
    if (foo) { bar(); }
    ```

    Improved code:
    ```typescript
    if (foo) {
        bar();
    }
    ```

    * In languages that have optional curly braces for some statements (e.g. C#, TypeScript), we prefer to add them even if the code block will only contain one line. This way, when the next developer adds more lines to it later it's less work for him and doesn't cause unnecessary git-blame noise..

    Example (with too cramped style):
    ```typescript
    if (foo)
        bar();
    ```

    Improved code:
    ```typescript
    if (foo) {
        bar();
    }
    ```

* Write code for a polyglot world: just as it is now an industry standard to write code in English (i.e. method/variable/class names, comments, etc) even when the entire team speaks a different native language. A major benefit of this convention is that it dramatically widens the hiring pool—an organization can recruit competent developers from anywhere in the world without requiring fluency in the local language spoken at the company's headquarters. We should apply the same logic to language-specific syntax, because the future is polyglot: software development teams (especially in this era of AI agents) will regularly work across many programming languages, and deep familiarity with any single language's quirks shouldn't be assumed. Therefore, prefer universally readable constructs over terse, language-specific idioms that save little boilerplate but may add significant confusion for anyone who is not already steeped in that particular language's culture.

    * Avoid spread/splat (`...`) operators where a clearer alternative exists; prefer named functions like `Array.from`.

        Example (bad):
        ```typescript
        const inputs = [...tableCell.children];
        ```

        Improved code:
        ```typescript
        const inputs = Array.from(tableCell.children);
        ```

        And actually, this is not just a readability recommendation, because apparently the spread operator can have performance issues, see: https://github.com/earendil-works/pi/pull/4463

    * Prefer named functions over slice operators (e.g. in F#: `[..]`, `[*..]`, `[..*]`) which are cryptic to developers unfamiliar with the language.

        Example (bad):
        ```fsharp
        let prefix = someString.[.. firstSpace - 1]
        ```

        Improved code:
        ```fsharp
        let prefix = someString.Substring(0, firstSpace)
        ```

    * Prefer named functions like `append` or `extend` over terse operators for combining collections. Operators such as `@` (used in F#, OCaml and other ML-family languages) or `+` (used in Python, Swift and others) are cryptic to anyone not steeped in that particular ecosystem.

        Example (bad):
        ```fsharp
        let combined = firstList @ secondList
        ```

        Improved code:
        ```fsharp
        let combined = List.append firstList secondList
        ```

    * Prefer destructuring/pattern matching on tuples instead of using magic functions (e.g. `fst` and `snd` in F#) or indexers (e.g. `[0]` and `[1]` in Python) which might be confusing or brittle, respectively.

        Example (bad):
        ```fsharp
        let name = fst userTuple
        let age = snd userTuple
        ```

        Improved code:
        ```fsharp
        let name, age = userTuple
        ```

        Example (bad):
        ```python
        name = user_tuple[0]
        age = user_tuple[1]
        ```

        Improved code:
        ```python
        name, age = user_tuple
        ```

    * Add parentheses to make operator precedence explicit, even when the language's precedence rules already dictate the evaluation order. A reader who does not know those rules by heart should not have to look them up.

        Example (bad):
        ```typescript
        const flag = a > 0 && b !== null || c;
        let result = a + b * c - d;
        ```

        Improved code:
        ```typescript
        const flag = (a > 0 && b !== null) || c;
        let result = a + (b * c) - d;
        ```

    * Use separate assignment statements instead of chained/collapsed assignments. Chained assignment may look compact, but its evaluation order is non-obvious to someone unfamiliar with the language, and some languages don't even support it at all.

        Example (bad):
        ```typescript
        let a = b = c = 0;
        ```

        Improved code:
        ```typescript
        let c = 0;
        let b = 0;
        let a = 0;
        ```

    * Prefer simple property access instead of destructuring when extracting a single value.

        Example (bad):
        ```typescript
        const { bar: foo } = object;
        ```

        Improved code:
        ```typescript
        const foo = object.bar;
        ```

        We used TypeScript in the above examples, especially to showcase the paradoxical fact of this ecosystem about the existence of an ESLint rule called `prefer-destructuring` that actually advocates for less readable code given that it conveys the developer to using destructuring even for single properties!

* Avoid typical bad practices like:

    * Magic numbers: avoid using unnamed numerical constants in software code, since this practice makes code hard to understand and maintain.

    Example (bad):
    ```fsharp
    let commitDescription =
        if commitLines.Length >= 3 then
            String.Join(Environment.NewLine, commitLines |> Seq.skip 2).Trim()
        else
            String.Empty
    ```

    Improved code:
    ```fsharp
    let commitDescription =
        // because the 1st is the commit msg title, and the 2nd is separation between title & body
        let linesToSkipToReachCommitMsgBody = 2

        if commitLines.Length > linesToSkipToReachCommitMsgBody then
            commitMsgBodyLines = commitLines |> Seq.skip linesToSkipToReachCommitMsgBody
            String.Join(Environment.NewLine, commitMsgBodyLines).Trim()
        else
            String.Empty
    ```

    * DRY (Don't Repeat Yourself): the DRY principle suggests that a piece of information should only be stored once in a project and referenced as needed, rather than being copied and pasted multiple times throughout the codebase.

    Example (bad):
    ```fsharp
    let preAuthInputMac =
        CalculateMacWithSHA3256
            preAuthInput
            ":hs_mac"

    ...

    let authInputMac =
        CalculateMacWithSHA3256
            authInput
            ":hs_mac"
    ```

    Improved code:
    ```fsharp
    let AuthenticationDigestCalculationKey = ":hs_mac"

    ...

    let preAuthInputMac =
        CalculateMacWithSHA3256
            preAuthInput
            AuthenticationDigestCalculationKey

    ...

    let authInputMac =
        CalculateMacWithSHA3256
            authInput
            AuthenticationDigestCalculationKey
    ```

    * Primitive Obsession: avoid overusing primitive types (strings, integers, arrays) where a richer type/object would be more appropriate.

    Example (bad):
    ```fsharp
    let saveFilePath = System.Console.ReadLine()

    let savedData = System.IO.File.ReadAllText saveFilePath
    ```

    Improved code:
    ```fsharp
    let saveFilePath =
        let saveFilePathInString =
            System.Console.ReadLine()
        System.IO.FileInfo saveFilePathInString

    let savedData = System.IO.File.ReadAllText saveFilePath.FullName
    ```

    * Discarding generic exceptions: catching all exceptions and discarding them is harmful because it can hide bugs. It's better to handle specific exception types (e.g. you can remove the `try` block and run the code in a debugging session to find out what type is the exception you're expecting).

    Example (bad):
    ```fsharp
    let Func (zipFile: FileInfo) =
        try
            ZipFile.Open(zipFile.FullName, ZipArchiveMode.Read) |> ignore<ZipArchive>
        with
        | ex ->
            Console.Error.WriteLine $"Found an issue with %s{zipFile.FullName}"
    ```

    Improved code:
    ```fsharp
    let Func (zipFile: FileInfo) =
        try
            ZipFile.Open(zipFile.FullName, ZipArchiveMode.Read) |> ignore<ZipArchive>
        with
        | :? FileNotFoundException -> 
            Console.Error.WriteLine $"%s{zipFile.FullName} was not found "
        | _ -> 
            reraise()
    ```

    * Empty catch‑all blocks: if you really can't use a specific exception in your catch block and the catch has to capture any kind of exception, DO NOT leave the catch block completely empty. At minimum, log the exception with its details. And if logging it truly doesn't make sense, include a comment explaining why logging is omitted, why a catch‑all is needed, and why it's safe to swallow the exception.

    Example (bad):
    ```csharp
    try {
        SomeOperation();
    } catch (Exception) {
    }
    ```

    Improved code (preferred — log the exception):
    ```csharp
    try {
        SomeOperation();
    } catch (Exception ex) {
        // This catch-all is intentional because the operation is non‑critical
        Logger.Warn(ex, "Non‑critical operation failed; proceeding without aborting.");
    }
    ```

    Improved code (when logging doesn't apply — explain why):
    ```csharp
    try {
        SomeOperation();
    } catch {
        // Logging is not appropriate here because the exception may contain
        // sensitive credential data that must not be written to logs.
        // This catch-all is needed because the third‑party library does not
        // document which exception types it throws, and the operation is
        // safe to skip without side effects.
    }
    ```

    * Catching preventable exceptions: some exceptions are avoidable and should be prevented by checks instead of being caught.

    Example (bad):
    ```fsharp
        try
            name.ComposeFullName(separator)
        with
        | :? ArgumentNullException ->
            Console.Error.WriteLine "There was a problem retreiving the name"
        | :? NullReferenceException ->
            Console.Error.WriteLine "There was a problem in the naming system, please report this bug"
    ```

    Improved code:
    ```fsharp
        if isNull name then
            Console.Error.WriteLine "There was a problem in the naming system, please report this bug"
        elif isNull separator then
            Console.Error.WriteLine "There was a problem retreiving the name"
        else
            name.ComposeFullName separator
    ```
    This approach tends to be more readable and maintainable.

    * Using Message and StackTrace properties of exception instead of ToString(): if using .NET, prefer `Exception.ToString()` when displaying exception info because it includes type, message, and stack trace. 

    Example (bad):
    ```csharp
    try {
        SomeOperation();
    } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        Console.Error.WriteLine(ex.StackTrace);
    }
    ```

    Improved code:
    ```csharp
    try {
        SomeOperation();
    } catch (Exception ex) {
        Console.Error.WriteLine(ex.ToString());
    }
    ```

    * Not benefiting from your type system: we use statically-typed languages (such as TypeScript and C#) to let the compiler protect us. See many examples of this below.

        * Use the type system rather than sentinel/edge values. For example, in C#, do not use `DateTime.MinValue` to denote absence of a date, or `-1` to denote absence of a number, use a nullable type instead.

        Example in C# (bad):
        ```csharp
        DateTime SomeFunction() {
            if (!SomeInnerFunction()) {
                return DateTime.MinValue;
            } else {
                return DateTime.Now;
            }
        }
        ```

        Improved code:
        ```csharp
        Nullable<DateTime> SomeFunction() {
            if (!SomeInnerFunction()) {
                return null;
            } else {
                return DateTime.Now;
            }
        }
        ```

        * If your language supports Option types (e.g. F#) natively or through libraries (e.g. TypeScript), then use these instead of nullable/nullish types.

        Example in F# (bad):
        ```fsharp
        let SomeFunction(): SomeObj =
            if not (SomeInnerFunction())
                null
            else
                SomeObj("init", 0)
        ```

        Improved code:
        ```fsharp
        let SomeFunction(): Option<SomeObj> =
            if not (SomeInnerFunction())
                None
            else
                Some <| SomeObj("init", 0)
        ```

        Example in TypeScript (bad):
        ```typescript
        function someFunction(): SomeObj | null | undefined {
            if (!(someInnerFunction())) {
                return null;
            } else {
                return new SomeObj("init", 0);
            }
        }
        ```

        Improved code:
        ```typescript
        import { Option, Some, None } from "fp-sdk";

        function someFunction(): Option<SomeObj> {
            if (!(someInnerFunction())) {
                return new None();
            } else {
                return new Some(new SomeObj("init", 0));
            }
        }
        ```

        * If the library you consume is not respecting our coding guidelines about nullish/option types, then protect yourself from it from the start when doing absence checks and do not hardcode nullish keywords.

        For example, in F#, use `Option.ofObj()` and then use a match against the result of this function.

        Example (bad):
        ```fsharp
        if SomeApiFromExternalLibrary.SomeSingletonStaticProperty = null then
            failwith "some kind of error..."
        UseExternalData(SomeApiFromExternalLibrary.SomeSingletonStaticProperty)
        ```

        Improved code (that avoids you having to type `null` but, most importantly, avoids a race condition):
        ```fsharp
        let someExternalData = Option.ofObj(SomeApiFromExternalLibrary.SomeSingletonStaticProperty)
        match someExternalData with
        | None ->
            failwith "some kind of error..."
        | Some data ->
            UseExternalData(data)
        ```

        In TypeScript, the equivalent of F#'s `Option.ofObj()` exists in `fp-sdk`'s: `Option.ofObj()`, but if you do not need to use the value and just check for absence, you can still avoid typing nullish keywords by using `Check.isNullOrUndefined()`:

        Example (bad):
        ```typescript
        if (foo === undefined || foo === null)
        {
            return 0;
        }
        return 1;
        ```

        Improved code:
        ```typescript
        import { Check } from "fp-sdk";

        if (Check.isNullOrUndefined(foo))
        {
            return 0;
        }
        return 1;
        ```

    * Abusing obscure operators or the excessive multi-facetedness of basic ones:

        * Do not use `!` for non-boolean values as a null-check; prefer explicit null checks.

        Example (bad):
        ```typescript
        if (!zipFile) {
            return;
        }
        ```

        Improved code:
        ```typescript
        if (zipFile === null) {
            return;
        }
        ```

        * Even if you're checking bool variables, still don't use `!` and be explicit about the comparison against true or false, otherwise if you're using a bad language that has truthiness issues (e.g. **falsy** elements), you might fall into a trap.

        Example (bad) because developer thinks that the condition will only be met when value is `false`:
        ```typescript
        if (!value) {
            ...
        }
        ```

        Improved code (that prevents a `0` or empty string `""` to meet the condition):
        ```typescript
        if (value === false) {
            ...
        }
        ```

        * Even if you're not using `!` be explicit about the comparison against true or false, otherwise with TypeScript your condition will also be met with **truthy** values such as `1`.

        Example (bad) because developer thinks that the condition will only be met when value is `false`:
        ```typescript
        if (value) {
            ...
        }
        ```

        Improved code:
        ```typescript
        if (value === true) {
            ...
        }
        ```

        NB: Please do this even if your language doesn't have these traps (e.g. F#), so that the code is more readable for people that are switching languages constantly.

        * Avoid recent terse C# operators like `??` and `??=` when they make the code less readable—explicit checks are clearer.

        Example (bad):
        ```csharp
        someField = someValue ?? throw new ArgumentNullException("someValue cannot be null");

        someVar ??= expression;
        ```

        Improved code:
        ```csharp
        if (someValue is null) {
            throw new ArgumentNullException("someValue cannot be null");
        }
        someField = someValue;

        if (someVar is null) {
            someVar = expression;
        }
        ```

* Don't add obvious comments that can be inferred from just reading the code. Use comments to explain why you're doing something, not what the code is doing. Sometimes extracting code into a well-named function removes the need for a comment.

    Example (bad):
    ```csharp
    // increment the counter
    counter++;

    // commit msg has body
    if (commitLines.Length > 2) {
        // get the user
        var user = GetUser(id);

        var freshData = FetchData();
        ...
    }
    ```

    Improved code:
    ```csharp
    counter++;

    var commitMsgHasBody = commitLines.Length > 2
    if (commitMsgHasBody) {
        // get the user in order to verify operation
        var user = GetUser(id);

        // fetch again because the API sometimes returns stale data on the first call
        var freshData = FetchData(user);
        ...
    }
    ```

* Add comments on top of code (on a line above it), not next to it, to avoid horizontal scrolling.

    Example (bad):
    ```csharp
    var retryCount = 3; // we retry 3 times because the service is flaky
    ```
    Improved code:
    ```csharp
    // we retry 3 times because the service is flaky
    var retryCount = 3;
    ```

* Do not commit commented code; if code is commented-out it should be removed. If there's a reason to keep it, explain that reason in a comment above the commented code.

    Example (bad):
    ```csharp
    // var oldValue = ComputeLegacy();
    var newValue = ComputeNew();
    ```

    Improved code (remove it entirely, or justify its presence):
    ```csharp
    var newValue = ComputeNew();
    ```

    Or, if there's a valid reason to keep the old code visible:
    ```csharp
    // keeping the legacy call commented until v3 migration is verified in production
    // var oldValue = ComputeLegacy();

    var newValue = ComputeNew();
    ```

* If you're in need of serialization/deserialization/marshalling: **DO NOT** use `JSON.NET` (aka `Newtonsoft.Json`) because it is considered a deprecated library in .NET (in favour of the new BCL one: `System.Text.Json`). `System.Text.Json` had some problems in past versions about DU serialization (e.g. F# DUs), but it seems to have been finally addressed, see https://github.com/dotnet/runtime/commit/3e902855154615baa4a2a7584db75beeeb0fad97. That said, it probably doesn't support JSON5 (funnily enough, `Newtonsoft.Json` supports already one of the best features of JSON5: comments; so maybe `System.Text.Json` does too?). Before adopting alternative libraries, take into account that JSON5 is more human-readable than JSON but at the expense of significant performance, and any such decision should be discussed with your team lead.
