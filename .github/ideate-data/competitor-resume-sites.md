# Competitor resume/portfolio site research

Maintained by `pipeline-stage0-ideate.yml`. Read on every ideate run; only
re-researched and rewritten when missing or older than ~60 days, to keep
routine runs cheap. Ranked roughly by how many actionable ideas each site
has yielded for this repo so far.

**Last updated:** 2026-08-17

## Ranked list

1. **Diogo Correia** — https://diogotc.com
   - Particle-effect hero background, sticky nav header, and (most relevant here)
     a clean work/experience **timeline** presentation — directly comparable to
     this repo's `WorkExperienceSection`/`work-experience-section` cards. Worth
     revisiting if we ever redesign that section's layout.

2. **Cassie Evans** — https://cassie.codes
   - Split layout: custom illustration paired with bold typography. Nav reflects
     the *breadth* of what she does (writing, speaking, workshops, projects) —
     not just job titles. Relevant to our `Projects` page: consider whether
     external talks/writing/OSS contributions belong alongside project links.

3. **Kenneth Jimmy** — https://kenjimmy.xyz
   - Framed/boxed (non-full-width) layout, dark/light mode switcher in the
     header, and a strategic above-the-fold CTA button for contact. The
     dark/light switcher is the standout idea — this site currently has no
     theme toggle on either client.

4. **Andrew McCarthy** — https://andrevv.com
   - Infinite-scroll sections with a header that hides on scroll-down and
     reappears on scroll-up. Nice-to-have polish, lower priority than content
     additions.

5. **La Playa (agency, portfolio-style)** — https://laplaya.studio
   - Two-column grid with hover-highlight, sticky sidebar with drop-down info
     reveals. Interaction pattern more suited to project galleries than a
     single-person resume; noted for future `Projects` page inspiration.

## Cross-cutting themes from broader research (not single-site-specific)

- **GitHub integration**: multiple 2026 roundups (Colorlib, Kickresume,
  Resumly) call out surfacing live GitHub activity (contribution graph, pinned
  repos, or a "view the code" link per project) as a high-trust signal —
  recruiters treat verifiable public code as more credible than a project
  description alone. This site's `Projects` page currently links out but
  doesn't surface any GitHub activity/stats directly.
- **Testimonials/social proof**: already implemented here (issue #25 gated
  feature) — ahead of most sites surveyed, which rarely have a real
  testimonials system.
- **Case studies over project lists**: roundups consistently note that 1-2
  detailed case studies (problem → approach → outcome) outperform a long flat
  list of thin project links. Could inform a future `Projects` page
  enhancement — write-up depth over link count.
- **Structured timeline**: consistently cited as a core resume-site element
  (career timeline, education, certs) — this repo already has this via
  `WorkExperienceSection`/`EducationSection`; no gap identified there.

Sources consulted: Colorlib "21 Best Developer Portfolio Websites" (2026),
general web search on developer portfolio best practices (2026), and a
search on resume-website feature patterns (GitHub integration, timelines,
case studies).
