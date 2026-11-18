# Matched iOS source promotion

The Hybrid pod uses an immutable commit, and the companion Package CLI must select the matching immutable plugin commit. Publish the reviewed Hybrid source first, then the plugin that references it, and expose the Package default only after both exact commits are available through their declared upstream Git URLs. Keep the referenced commits reachable. Other SDK pod sources retain their existing release-branch policy; an app's Podfile.lock records the resolved dependency graph.

Review branches may reference unpublished companion commits. If squash or rebase changes a commit ID, replace the dependent pin with the final upstream ID and repeat fresh-consumer installation through the public Git URLs before promotion. Local source overrides or Git URL mappings verify only the mapped source set and do not establish public availability. Coordinate the development-branch transition so consumers are not exposed to an unmatched intermediate source set.

## Parser workflow promotion

The default-branch CI bootstrap covers numbered release branches while the parser tests remain on the upgrade branch. The workflow shipped with those tests covers `dev`, numbered release branches and `master`; promote that workflow together with its test script and dependencies. This avoids running an absent test harness on the older default branch. The manual route requires the requested PR's exact reviewed commit SHA before installing dependencies or running tests.
