FROM postgres:16-alpine

COPY ensure.sql /migrations/ensure.sql
COPY entrypoint.sh /usr/local/bin/gaia-entrypoint.sh
RUN chmod +x /usr/local/bin/gaia-entrypoint.sh

ENTRYPOINT ["gaia-entrypoint.sh"]
