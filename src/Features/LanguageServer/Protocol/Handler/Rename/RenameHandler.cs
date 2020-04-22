﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportLspMethod(LSP.Methods.TextDocumentRenameName), Shared]
    internal class RenameHandler : IRequestHandler<LSP.RenameParams, WorkspaceEdit>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RenameHandler()
        {
        }

        public async Task<WorkspaceEdit> HandleRequestAsync(Solution solution, RenameParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            WorkspaceEdit workspaceEdit = null;
            var document = solution.GetDocumentFromURI(request.TextDocument.Uri);
            if (document != null)
            {
                var renameService = document.Project.LanguageServices.GetService<IEditorInlineRenameService>();
                var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

                var renameInfo = await renameService.GetRenameInfoAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (!renameInfo.CanRename)
                {
                    return workspaceEdit;
                }

                var renameLocationSet = await renameInfo.FindRenameLocationsAsync(solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(request.NewName, solution.Workspace.Options, cancellationToken).ConfigureAwait(false);

                var newSolution = renameReplacementInfo.NewSolution;
                var solutionChanges = newSolution.GetChanges(solution);

                // Merge changes in linked files.  Will result in linked file documents having the same changes in all linked documents.
                // Once the changes are the same across linked files, we can take the distinct changes by file uri
                var solutionWithLinkedFileChangesMerged = newSolution.WithMergedLinkedFileChangesAsync(solution, solutionChanges, cancellationToken: cancellationToken).Result;
                solutionChanges = solutionWithLinkedFileChangesMerged.GetChanges(solution);
                var changedDocuments = solutionChanges
                    .GetProjectChanges()
                    .SelectMany(p => p.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true))
                    .GroupBy(docId => newSolution.GetRequiredDocument(docId).FilePath).Select(group => group.First());

                var documentEdits = new ArrayBuilder<TextDocumentEdit>();
                foreach (var docId in changedDocuments)
                {
                    var oldDoc = solution.GetRequiredDocument(docId);
                    var newDoc = newSolution.GetRequiredDocument(docId);

                    var textChanges = await newDoc.GetTextChangesAsync(oldDoc, cancellationToken).ConfigureAwait(false);
                    var oldText = await oldDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var textDocumentEdit = new TextDocumentEdit
                    {
                        TextDocument = new VersionedTextDocumentIdentifier { Uri = newDoc.GetURI() },
                        Edits = textChanges.Select(tc => ProtocolConversions.TextChangeToTextEdit(tc, oldText)).ToArray()
                    };
                    documentEdits.Add(textDocumentEdit);
                }

                workspaceEdit = new WorkspaceEdit { DocumentChanges = documentEdits.ToArrayAndFree() };
            }

            return workspaceEdit;
        }
    }
}
