﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.LanguageServices
{
    /// <summary>
    /// Abstract implementation of the C# and VB embedded language providers.
    /// </summary>
    internal abstract class AbstractFeaturesEmbeddedLanguagesProvider : AbstractEmbeddedLanguagesProvider, IFeaturesEmbeddedLanguagesProvider
    {
        private readonly ImmutableArray<IFeaturesEmbeddedLanguage> _embeddedLanguages;
         
        protected AbstractFeaturesEmbeddedLanguagesProvider(
            int stringLiteralTokenKind,
            int interpolatedTextTokenKind,
            ISyntaxFactsService syntaxFacts,
            ISemanticFactsService semanticFacts,
            IVirtualCharService virtualCharService)
            : base(stringLiteralTokenKind, interpolatedTextTokenKind, syntaxFacts, semanticFacts, virtualCharService)
        {
            _embeddedLanguages = ImmutableArray.Create<IFeaturesEmbeddedLanguage>(
                new RegexFeaturesEmbeddedLanguage(stringLiteralTokenKind, syntaxFacts, semanticFacts, virtualCharService));
        }

        public new ImmutableArray<IFeaturesEmbeddedLanguage> GetEmbeddedLanguages()
            => _embeddedLanguages;
    }
}
