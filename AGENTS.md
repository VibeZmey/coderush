# CodeRush

After changing the project, rebuild the affected Docker Compose services and recreate their containers before handing off the work. Run Compose from the repository root with `docker compose --project-directory src up -d --build <services>`, selecting only the services affected by the changes. Verify their state with `docker compose --project-directory src ps` and check logs if a service fails to start.
