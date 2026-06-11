module FileConventions.Test.RestOfTests

open System
open System.IO

open NUnit.Framework
open NUnit.Framework.Constraints
open Fsdk
open Fsdk.Process

open FileConventions

open type FileConventions.EolAtEof

[<SetUp>]
let Setup() =
    ()

let dummyFilesDirectory =
    DirectoryInfo(Path.Combine(__SOURCE_DIRECTORY__, "DummyFiles"))

[<Test>]
let HasCorrectShebangTest1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithoutShebang.fsx"
            )
        ))

    Assert.That(HasCorrectShebang fileInfo, Is.EqualTo false)


[<Test>]
let HasCorrectShebangTest2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyWithShebang.fsx")
        ))

    Assert.That(HasCorrectShebang fileInfo, Is.EqualTo true)


[<Test>]
let HasCorrectShebangTest3() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithWrongShebang.fsx"
            )
        ))

    Assert.That(HasCorrectShebang fileInfo, Is.EqualTo false)


[<Test>]
let HasCorrectShebangTest4() =
    let fileInfo =
        (FileInfo(Path.Combine(dummyFilesDirectory.FullName, "DummyEmpty.fsx")))

    Assert.That(HasCorrectShebang fileInfo, Is.EqualTo false)


[<Test>]
let MixedLineEndingsTest1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithMixedLineEndings"
            )
        ))

    Assert.That(MixedLineEndings fileInfo, Is.EqualTo true)


[<Test>]
let MixedLineEndingsTest2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyWithLFLineEndings")
        ))

    Assert.That(MixedLineEndings fileInfo, Is.EqualTo false)


[<Test>]
let MixedLineEndingsTest3() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithCRLFLineEndings"
            )
        ))

    Assert.That(MixedLineEndings fileInfo, Is.EqualTo false)


[<Test>]
let DetectUnpinnedVersionsInGitHubCI1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithLatestTag.yml"
            )
        ))

    Assert.That(DetectUnpinnedVersionsInGitHubCI fileInfo, Is.EqualTo true)


[<Test>]
let DetectUnpinnedVersionsInGitHubCI2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithoutLatestTag.yml"
            )
        ))

    Assert.That(DetectUnpinnedVersionsInGitHubCI fileInfo, Is.EqualTo false)

[<Test>]
let DetectUnpinnedVersionsInGitHubCI3() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithLatestTagOnContainer.yml"
            )
        ))

    Assert.That(DetectUnpinnedVersionsInGitHubCI fileInfo, Is.EqualTo true)

[<Test>]
let DetectUnpinnedVersionsInGitHubCI4() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithLatestTagButAlsoContainer.yml"
            )
        ))

    Assert.That(DetectUnpinnedVersionsInGitHubCI fileInfo, Is.EqualTo false)

[<Test>]
let DetectUnpinnedVersionsInGitHubCI5() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyGitHubActionWithoutJobs.yml"
            )
        ))

    Assert.That(DetectUnpinnedVersionsInGitHubCI fileInfo, Is.EqualTo false)

[<Test>]
let DetectUnpinnedDotnetToolInstallVersions1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithUnpinnedDotnetToolInstallVersion.yml"
            )
        ))

    Assert.That(
        DetectUnpinnedDotnetToolInstallVersions fileInfo,
        Is.EqualTo true
    )


[<Test>]
let DetectAsteriskInPackageReferenceItems1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyFsprojWithAsterisk.fsproj"
            )
        ))

    Assert.That(DetectAsteriskInPackageReferenceItems fileInfo, Is.EqualTo true)


[<Test>]
let DetectAsteriskInPackageReferenceItems2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyFsprojWithoutAsterisk.fsproj"
            )
        ))

    Assert.That(
        DetectAsteriskInPackageReferenceItems fileInfo,
        Is.EqualTo false
    )


[<Test>]
let MissingVersionsInNugetPackageReferencesTest1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithMissingVersionsInNugetPackageReferences.fsx"
            )
        ))

    Assert.That(
        DetectMissingVersionsInNugetPackageReferences fileInfo,
        Is.EqualTo true
    )


[<Test>]
let MissingVersionsInNugetPackageReferencesTest2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithoutMissingVersionsInNugetPackageReferences.fsx"
            )
        ))

    Assert.That(
        DetectMissingVersionsInNugetPackageReferences fileInfo,
        Is.EqualTo false
    )


[<Test>]
let MissingVersionsInNugetPackageReferencesTest3() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithoutNugetPackageReferences.fsx"
            )
        ))

    Assert.That(
        DetectMissingVersionsInNugetPackageReferences fileInfo,
        Is.EqualTo false
    )


[<Test>]
let EolAtEofTest1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyWithEolAtEof.txt")
        ))

    Assert.That(EolAtEof fileInfo, Is.EqualTo True)


[<Test>]
let EolAtEofTest2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWithoutEolAtEof.txt"
            )
        ))

    Assert.That(EolAtEof fileInfo, Is.EqualTo False)


[<Test>]
let EolAtEofTest3() =
    let fileInfo =
        (FileInfo(Path.Combine(dummyFilesDirectory.FullName, "someLib.dll")))

    Assert.That(EolAtEof fileInfo, Is.EqualTo NotApplicable)


[<Test>]
let HasBinaryContentTest1() =
    let fileInfo =
        (FileInfo(Path.Combine(dummyFilesDirectory.FullName, "someLib.dll")))

    Assert.That(HasBinaryContent fileInfo, Is.EqualTo true)


[<Test>]
let HasBinaryContentTest2() =
    let fileInfo =
        (FileInfo(Path.Combine(dummyFilesDirectory.FullName, "white.png")))

    Assert.That(HasBinaryContent fileInfo, Is.EqualTo true)


[<Test>]
let HasBinaryContentTest3() =
    let fileInfo =
        (FileInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyNonBinaryFile.txt")
        ))

    Assert.That(HasBinaryContent fileInfo, Is.EqualTo false)


[<Test>]

let DetectInconsistentVersionsInGitHubCIWorkflow1() =
    let fileInfo =
        (Seq.singleton(
            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithSamePulumiVersion.yml"
                )
            )
        ))

    Assert.That(
        DetectInconsistentVersionsInGitHubCIWorkflow fileInfo,
        Is.EqualTo false
    )


[<Test>]
let DetectInconsistentVersionsInGitHubCIWorkflow2() =
    let fileInfo =
        (Seq.singleton(
            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithoutSamePulumiVersion.yml"
                )
            )
        ))

    Assert.That(
        DetectInconsistentVersionsInGitHubCIWorkflow fileInfo,
        Is.EqualTo true
    )


[<Test>]
let DetectInconsistentVersionsInGitHubCIWorkflow3() =
    let fileInfo =
        (Seq.singleton(
            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithoutSameSetupPulumiVersion.yml"
                )
            )
        ))

    Assert.That(
        DetectInconsistentVersionsInGitHubCIWorkflow fileInfo,
        Is.EqualTo true
    )


[<Test>]
let DetectInconsistentVersionsInGitHubCIWorkflow4() =
    let fileInfo =
        (Seq.singleton(
            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithSameSetupPulumiVersion.yml"
                )
            )
        ))

    Assert.That(
        DetectInconsistentVersionsInGitHubCIWorkflow fileInfo,
        Is.EqualTo false
    )


[<Test>]
let DetectInconsistentVersionsInGitHubCIWorkflow5() =
    let fileInfo =
        (seq {

            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithSetupPulumiVersionV2.0.0.yml"
                )
            )

            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithSetupPulumiVersionV2.0.1.yml"
                )
            )

        })

    Assert.That(
        DetectInconsistentVersionsInGitHubCIWorkflow fileInfo,
        Is.EqualTo true
    )


[<Test>]
let DetectInconsistentVersionsInGitHubCIWorkflow6() =
    let fileInfo =
        (Seq.singleton(
            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithoutSameCheckoutVersion.yml"
                )
            )
        ))

    Assert.That(
        DetectInconsistentVersionsInGitHubCIWorkflow fileInfo,
        Is.EqualTo true
    )


[<Test>]
let DetectInconsistentVersionsInGitHubCIWorkflow7() =
    let fileInfo =
        (Seq.singleton(
            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyCIWithoutSameNodeVersion.yml"
                )
            )
        ))

    Assert.That(
        DetectInconsistentVersionsInGitHubCIWorkflow fileInfo,
        Is.EqualTo true
    )


[<Test>]
let DetectInconsistentVersionsInGitHubCI1() =
    let fileInfo =
        DirectoryInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyWorkflows")
        )

    Assert.That(DetectInconsistentVersionsInGitHubCI fileInfo, Is.EqualTo true)

[<Test>]
let DetectInconsistentVersionsInGitHubCI2() =
    let fileInfo =
        DirectoryInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyWorkflowsWithEnv")
        )

    Assert.That(DetectInconsistentVersionsInGitHubCI fileInfo, Is.EqualTo true)

[<Test>]
let DetectInconsistentVersionsInGitHubCI3() =
    let fileInfo =
        DirectoryInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyWorkflowsWithEnvPrefixed"
            )
        )

    Assert.That(DetectInconsistentVersionsInGitHubCI fileInfo, Is.EqualTo true)

[<Test>]
let DetectInconsistentVersionsInNugetRefsInFSharpScripts1() =
    let fileInfos =
        (seq {

            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyFsharpScriptWithFsdkVersion0.6.0.fsx"
                )
            )

            FileInfo(
                Path.Combine(
                    dummyFilesDirectory.FullName,
                    "DummyFsharpScriptWithFsdkVersion0.6.1.fsx"
                )
            )

        })

    Assert.That(
        DetectInconsistentVersionsInNugetRefsInFSharpScripts fileInfos,
        Is.EqualTo true
    )

[<Test>]
let DetectInconsistentVersionsInFSharpScripts1() =
    let fileInfo =
        DirectoryInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyScripts")
        )

    Assert.That(
        DetectInconsistentVersionsInFSharpScripts fileInfo None,
        Is.EqualTo true
    )


[<Test>]
let DetectInconsistentVersionsInFSharpScripts2() =
    let dir =
        DirectoryInfo(
            Path.Combine(dummyFilesDirectory.FullName, "DummyScripts")
        )

    Assert.That(
        DetectInconsistentVersionsInFSharpScripts
            dir
            (Some(Seq.singleton "DummyScripts")),
        Is.EqualTo false
    )

[<Test>]
let DetectInconsistentVersionsInFSharpScriptsAndProjects() =
    let dir =
        DirectoryInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyScriptsAndProjects"
            )
        )

    let fsxFiles =
        Directory.EnumerateFiles(
            dir.FullName,
            "*.fsx",
            SearchOption.AllDirectories
        )
        |> Seq.map FileInfo

    let fsprojFiles =
        Directory.EnumerateFiles(
            dir.FullName,
            "*.fsproj",
            SearchOption.AllDirectories
        )
        |> Seq.map FileInfo

    Assert.That(
        CombinedVersionCheck.DetectInconsistentVersionsInNugetRefsInFSharpScripts
            fsxFiles
            fsprojFiles,
        Is.EqualTo true
    )


[<Test>]
let NonVerboseFlagsInGitHubCI1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithNonVerboseFlag.yml"
            )
        ))

    Assert.That(NonVerboseFlags fileInfo, Is.EqualTo true)


[<Test>]
let NonVerboseFlagsInGitHubCI2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithoutNonVerboseFlags.yml"
            )
        ))

    Assert.That(NonVerboseFlags fileInfo, Is.EqualTo false)


[<Test>]
let NonVerboseFlagsInGitHubCI3() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithAcceptedNonVerboseFlag1.yml"
            )
        ))

    Assert.That(NonVerboseFlags fileInfo, Is.EqualTo false)


[<Test>]
let NonVerboseFlagsInGitHubCI4() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyScriptWithNonVerboseFlag.fsx"
            )
        ))

    Assert.That(NonVerboseFlags fileInfo, Is.EqualTo true)


[<Test>]
let NonVerboseFlagsInGitHubCI5() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyScriptWithoutNonVerboseFlag.fsx"
            )
        ))

    Assert.That(NonVerboseFlags fileInfo, Is.EqualTo false)


[<Test>]
let NonVerboseFlagsInGitHubCI6() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                dummyFilesDirectory.FullName,
                "DummyCIWithAcceptedNonVerboseFlag2.yml"
            )
        ))

    Assert.That(NonVerboseFlags fileInfo, Is.EqualTo false)


[<Test>]
let IsExecutableTest1() =
    let filePath =
        Path.Combine(__SOURCE_DIRECTORY__, "DummyFiles", "DummyExecutable.fsx")

    Fsdk
        .Process
        .Execute(
            {
                Command = "chmod"
                Arguments = sprintf "+x %s" filePath
            },
            Echo.All
        )
        .UnwrapDefault()
    |> ignore<string>

    let fileInfo = (FileInfo filePath)
    Assert.That(IsExecutable fileInfo, Is.EqualTo true)


[<Test>]
let IsExecutableTest2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyNotExecutable.fs"
            )
        ))

    Assert.That(IsExecutable fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest1() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptBadExample1.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo true)

[<Test>]
let ContainsUnacceptableTypeScriptTest2() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample1.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest3() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample2.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest4() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample3.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest5() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample4.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest6() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample5.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest7() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample6.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest8() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample7.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest9() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptBadExample2.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo true)

[<Test>]
let ContainsUnacceptableTypeScriptTest10() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample8.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest11() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample9.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest12() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample10.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)

[<Test>]
let ContainsUnacceptableTypeScriptTest13() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptBadExample3.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo true)

[<Test>]
let ContainsUnacceptableTypeScriptTest14() =
    let fileInfo =
        (FileInfo(
            Path.Combine(
                __SOURCE_DIRECTORY__,
                "DummyFiles",
                "DummyUnacceptableTypeScriptGoodExample11.ts"
            )
        ))

    Assert.That(ContainsUnacceptableTypeScript fileInfo, Is.EqualTo false)
