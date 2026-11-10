using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mystery.Interrogation
{
    // Author-owned 2D investigation locations and searchable hotspots.
    // Separate from the case truth: never use player text as an item source.
    [Serializable]
    public sealed class InvestigationCatalog
    {
        public string caseId;
        public int theme;
        public InvestigationLocation[] locations = Array.Empty<InvestigationLocation>();
    }

    [Serializable]
    public sealed class InvestigationLocation
    {
        public string id;
        public string title;
        public string description;
        public InvestigationHotspot[] spots = Array.Empty<InvestigationHotspot>();
    }

    [Serializable]
    public sealed class InvestigationHotspot
    {
        public string id;
        public string label;
        public string observation;
        public string evidenceId;
        public string[] requiresEvidenceIds = Array.Empty<string>();
    }

    public static class InvestigationCatalogRules
    {
        // Unapproved files are viewable only by authors as JSON, never by runtime UI.
        // Check every registry link to avoid unlocking arbitrary or duplicated evidence.
        public static string Validate(InvestigationCatalog catalog, CaseFile file)
        {
            if (catalog == null || file == null || catalog.theme != file.theme
                || !string.Equals(catalog.caseId, file.caseId, StringComparison.Ordinal))
                return "사건과 조사 자료의 테마 또는 ID가 일치하지 않습니다.";
            if (catalog.locations == null || catalog.locations.Length == 0)
                return "조사 장소가 작성되지 않았습니다.";

            var places = new HashSet<string>(StringComparer.Ordinal);
            var hotspots = new HashSet<string>(StringComparer.Ordinal);
            var mappedEvidence = new HashSet<string>(StringComparer.Ordinal);
            foreach (var location in catalog.locations)
            {
                if (location == null || string.IsNullOrWhiteSpace(location.id)
                    || string.IsNullOrWhiteSpace(location.title)
                    || !places.Add(location.id))
                    return "조사 장소 ID 또는 이름이 누락/중복되었습니다.";
                if (location.spots == null || location.spots.Length == 0)
                    return "장소의 조사 지점이 비어 있습니다: " + location.id;
                foreach (var spot in location.spots)
                {
                    if (spot == null || string.IsNullOrWhiteSpace(spot.id)
                        || string.IsNullOrWhiteSpace(spot.label)
                        || string.IsNullOrWhiteSpace(spot.observation)
                        || !hotspots.Add(spot.id))
                        return "조사 지점 ID·이름·설명이 누락/중복되었습니다.";
                    if (!string.IsNullOrWhiteSpace(spot.evidenceId))
                    {
                        if (!InterrogationResolver.KnownEvidence(file, spot.evidenceId)
                            || !mappedEvidence.Add(spot.evidenceId))
                            return "조사 지점의 증거 ID가 미등록/중복 상태입니다: " + spot.id;
                    }
                    if (spot.requiresEvidenceIds != null)
                        foreach (var required in spot.requiresEvidenceIds)
                            if (!InterrogationResolver.KnownEvidence(file, required)
                                || string.Equals(required, spot.evidenceId, StringComparison.Ordinal))
                                return "조사 조건의 증거 ID가 잘못되었습니다: " + spot.id;
                }
            }

            if (file.evidence != null)
                foreach (var clue in file.evidence)
                    if (clue == null || !mappedEvidence.Contains(clue.id))
                        return "사건 증거의 조사 지점이 연결되지 않았습니다.";
            return null;
        }
    }
}
